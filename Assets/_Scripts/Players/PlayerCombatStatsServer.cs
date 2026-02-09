using UltimateDungeon.Items;
using Unity.Netcode;
using UnityEngine;

// NOTE: We intentionally avoid `using UltimateDungeon.Combat;` and
// `using UltimateDungeon.Items;` together because both namespaces define a
// `DamageType` enum (and potentially other overlapping names). To prevent
// CS0104 ambiguity, we alias the two DamageType enums explicitly.
using CombatDamageType = UltimateDungeon.Combat.DamageType;
using ItemDamageType = UltimateDungeon.Items.DamageType;

namespace UltimateDungeon.Players
{
    /// <summary>
    /// PlayerCombatStatsServer
    /// ----------------------
    /// Server-authoritative combat stat aggregation for a Player.
    ///
    /// WHY THIS EXISTS
    /// - Items/Affixes define player power, but Combat Core must not read items directly.
    /// - This component computes a single snapshot from equipped items + affixes.
    /// - Combat systems should read ONLY this snapshot (or a facade getter).
    ///
    /// AUTHORITY
    /// - Server only. Clients do not compute combat stats.
    ///
    /// CURRENT SCOPE
    /// - Weapon: min/max damage, swing speed, stamina per swing, damage type.
    /// - Affixes: HCI, DCI, DI, SwingSpeed, resists.
    /// - Base resists: armor/shield authored resists (if present).
    ///
    /// IMPORTANT
    /// - This script should remain a pure aggregator. Do NOT add combat execution here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCombatStatsServer : MonoBehaviour
    {
        // --------------------------------------------------------------------
        // References
        // --------------------------------------------------------------------

        [Header("References")]
        [SerializeField] private PlayerEquipmentComponent equipment;

        // --------------------------------------------------------------------
        // Debug
        // --------------------------------------------------------------------

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // --------------------------------------------------------------------
        // Snapshot
        // --------------------------------------------------------------------

        private PlayerCombatStatsSnapshot _snapshot;

        /// <summary>
        /// The latest computed snapshot.
        /// </summary>
        public PlayerCombatStatsSnapshot Snapshot => _snapshot;

        public event System.Action<PlayerCombatStatsSnapshot> SnapshotChanged;


        // --------------------------------------------------------------------
        // Unity lifecycle
        // --------------------------------------------------------------------

        private void Awake()
        {
            // Always start from a safe baseline so other systems can query even
            // before equipment is ready.
            _snapshot = CreateDefaultSnapshot();
        }

        private void Reset()
        {
            // Convenience: auto-wire in editor.
            equipment = GetComponentInChildren<PlayerEquipmentComponent>(true);
        }

        private void OnEnable()
        {
            // Only the server should compute authoritative stats.
            if (!IsServerActive())
                return;

            if (equipment == null)
                equipment = GetComponentInChildren<PlayerEquipmentComponent>(true);

            if (equipment != null)
                equipment.OnEquipmentChanged += HandleEquipmentChanged;

            RecomputeSnapshot("enable");
        }

        private void OnDisable()
        {
            if (equipment != null)
                equipment.OnEquipmentChanged -= HandleEquipmentChanged;
        }

        private void HandleEquipmentChanged()
        {
            RecomputeSnapshot("equipment-changed");
        }

        // --------------------------------------------------------------------
        // Authority helpers
        // --------------------------------------------------------------------

        private static bool IsServerActive()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        }

        // --------------------------------------------------------------------
        // Aggregation
        // --------------------------------------------------------------------

        /// <summary>
        /// Recomputes the snapshot from equipped items and their affixes.
        /// This should be called whenever equipment changes.
        /// </summary>
        private void RecomputeSnapshot(string reason)
        {
            if (!IsServerActive())
                return;

            var snapshot = CreateDefaultSnapshot();

            // Percent-style totals (stored as 0..1 in the snapshot).
            float hciPercentTotal = 0f;
            float dciPercentTotal = 0f;
            float diPercentTotal = 0f;

            // IMPORTANT: Combat_SwingSpeed stacking policy is HighestOnly.
            // So we take MAX, not SUM.
            float swingSpeedPercentHighest = 0f;

            // Flat resists (sum).
            int resistPhysical = 0;
            int resistFire = 0;
            int resistCold = 0;
            int resistPoison = 0;
            int resistEnergy = 0;

            if (equipment != null)
            {
                // Walk every equip slot and accumulate contributions.
                foreach (UltimateDungeon.Items.EquipSlot slot in System.Enum.GetValues(typeof(UltimateDungeon.Items.EquipSlot)))
                {
                    if (slot == UltimateDungeon.Items.EquipSlot.None)
                        continue;

                    // Pull equipped runtime instance + its authored definition.
                    if (!equipment.TryGetEquippedItem(slot, out var instance, out var def))
                        continue;

                    // --------------------------------------------------------
                    // 1) Base stats from ItemDef
                    // --------------------------------------------------------
                    if (def != null)
                    {
                        // NOTE:
                        // Many projects represent family-specific data (armor/weapon)
                        // as structs that exist on all ItemDefs, defaulting to zero.
                        // If your ItemDef uses nullable blocks instead, add guards by
                        // ItemFamily / subtype before reading these fields.
                        resistPhysical += def.armor.resistPhysical;
                        resistFire += def.armor.resistFire;
                        resistCold += def.armor.resistCold;
                        resistPoison += def.armor.resistPoison;
                        resistEnergy += def.armor.resistEnergy;

                        // Weapon stats only come from Mainhand weapon.
                        if (slot == UltimateDungeon.Items.EquipSlot.Mainhand && def.family == UltimateDungeon.Items.ItemFamily.Mainhand)
                        {
                            snapshot.WeaponName = def.displayName;
                            snapshot.WeaponMinDamage = def.weapon.minDamage;
                            snapshot.WeaponMaxDamage = def.weapon.maxDamage;
                            snapshot.WeaponSwingSpeedSeconds = def.weapon.swingSpeedSeconds;
                            snapshot.WeaponStaminaCostPerSwing = def.weapon.staminaCostPerSwing;

                            // Bridge ItemDamageType -> CombatDamageType.
                            // We keep the translation explicit to avoid enum drift.
                            // If these enums ever diverge, replace this cast with a
                            // mapping function.
                            snapshot.WeaponDamageType = (CombatDamageType)(ItemDamageType)def.weapon.damageType;
                        }
                    }

                    // --------------------------------------------------------
                    // 2) Affix contributions from ItemInstance
                    // --------------------------------------------------------
                    if (instance != null && instance.affixes != null)
                    {
                        foreach (var affix in instance.affixes)
                        {
                            switch (affix.id)
                            {
                                // --- Combat ---
                                case UltimateDungeon.Items.AffixId.Combat_HitChance:
                                    // Percent (0..45) stored as float magnitude.
                                    hciPercentTotal += affix.magnitude;
                                    break;

                                case UltimateDungeon.Items.AffixId.Combat_DefenseChance:
                                    dciPercentTotal += affix.magnitude;
                                    break;

                                case UltimateDungeon.Items.AffixId.Combat_DamageIncrease:
                                    diPercentTotal += affix.magnitude;
                                    break;

                                case UltimateDungeon.Items.AffixId.Combat_SwingSpeed:
                                    // HighestOnly (per ITEM_AFFIX_CATALOG).
                                    swingSpeedPercentHighest = Mathf.Max(swingSpeedPercentHighest, affix.magnitude);
                                    break;

                                // --- Resists ---
                                case UltimateDungeon.Items.AffixId.Resist_Physical:
                                    resistPhysical += Mathf.RoundToInt(affix.magnitude);
                                    break;

                                case UltimateDungeon.Items.AffixId.Resist_Fire:
                                    resistFire += Mathf.RoundToInt(affix.magnitude);
                                    break;

                                case UltimateDungeon.Items.AffixId.Resist_Cold:
                                    resistCold += Mathf.RoundToInt(affix.magnitude);
                                    break;

                                case UltimateDungeon.Items.AffixId.Resist_Poison:
                                    resistPoison += Mathf.RoundToInt(affix.magnitude);
                                    break;

                                case UltimateDungeon.Items.AffixId.Resist_Energy:
                                    resistEnergy += Mathf.RoundToInt(affix.magnitude);
                                    break;
                            }
                        }
                    }
                }
            }

            // ------------------------------------------------------------
            // Finalize snapshot
            // ------------------------------------------------------------

            snapshot.ResistPhysical = resistPhysical;
            snapshot.ResistFire = resistFire;
            snapshot.ResistCold = resistCold;
            snapshot.ResistPoison = resistPoison;
            snapshot.ResistEnergy = resistEnergy;

            // Convert authored percent magnitudes (e.g. 15) into 0..1 scalars.
            snapshot.AttackerHciPct = hciPercentTotal * 0.01f;
            snapshot.DefenderDciPct = dciPercentTotal * 0.01f;
            snapshot.DamageIncreasePct = diPercentTotal * 0.01f;

            // Apply swing speed reduction.
            // Convention in this project: Combat_SwingSpeed is a percent that
            // reduces time, i.e., 20% swing speed -> 0.8x swing time.
            if (Mathf.Abs(swingSpeedPercentHighest) > 0.001f)
            {
                float multiplier = Mathf.Max(0.1f, 1f - swingSpeedPercentHighest * 0.01f);
                snapshot.WeaponSwingSpeedSeconds = Mathf.Max(0.1f, snapshot.WeaponSwingSpeedSeconds * multiplier);
            }

            _snapshot = snapshot;
            SnapshotChanged?.Invoke(_snapshot);


            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[PlayerCombatStatsServer] Recomputed ({reason}) " +
                    $"weapon='{_snapshot.WeaponName}' dmg={_snapshot.WeaponMinDamage}-{_snapshot.WeaponMaxDamage} " +
                    $"swing={_snapshot.WeaponSwingSpeedSeconds:0.00}s sta={_snapshot.WeaponStaminaCostPerSwing} " +
                    $"type={_snapshot.WeaponDamageType} " +
                    $"resists=P{_snapshot.ResistPhysical} F{_snapshot.ResistFire} C{_snapshot.ResistCold} " +
                    $"Po{_snapshot.ResistPoison} E{_snapshot.ResistEnergy} " +
                    $"hci={_snapshot.AttackerHciPct:0.00} dci={_snapshot.DefenderDciPct:0.00} di={_snapshot.DamageIncreasePct:0.00}",
                    this);
            }
        }

        // --------------------------------------------------------------------
        // Public query helpers (combat-facing)
        // --------------------------------------------------------------------

        public WeaponDamageRange GetWeaponMinMaxDamage()
        {
            return new WeaponDamageRange(_snapshot.WeaponMinDamage, _snapshot.WeaponMaxDamage);
        }

        public float GetSwingTimeSeconds()
        {
            return _snapshot.WeaponSwingSpeedSeconds;
        }

        public int GetStaminaCostPerSwing()
        {
            return _snapshot.WeaponStaminaCostPerSwing;
        }

        public ResistSnapshot GetResists()
        {
            return new ResistSnapshot(
                _snapshot.ResistPhysical,
                _snapshot.ResistFire,
                _snapshot.ResistCold,
                _snapshot.ResistPoison,
                _snapshot.ResistEnergy);
        }

        public HitChanceSnapshot GetHitChanceStats()
        {
            return new HitChanceSnapshot(_snapshot.AttackerHciPct, _snapshot.DefenderDciPct);
        }

        public float GetDamageIncreasePct()
        {
            return _snapshot.DamageIncreasePct;
        }

        public CombatDamageType GetWeaponDamageType()
        {
            return _snapshot.WeaponDamageType;
        }

        public bool GetCanAttack()
        {
            return _snapshot.CanAttack;
        }

        // --------------------------------------------------------------------
        // Defaults
        // --------------------------------------------------------------------

        private static PlayerCombatStatsSnapshot CreateDefaultSnapshot()
        {
            return new PlayerCombatStatsSnapshot
            {
                // Action gates (wired later via Status system).
                CanAttack = true,
                CanCast = true,
                CanMove = true,
                CanBandage = true,

                // Unarmed baseline.
                WeaponName = "Unarmed",
                WeaponMinDamage = 1,
                WeaponMaxDamage = 4,
                WeaponSwingSpeedSeconds = 2.0f,
                WeaponStaminaCostPerSwing = 0,
                WeaponDamageType = CombatDamageType.Physical,
            };
        }
    }
}
