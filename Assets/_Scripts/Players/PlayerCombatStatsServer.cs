using Unity.Netcode;
using UnityEngine;
using UltimateDungeon.Combat;
using UltimateDungeon.Items;

namespace UltimateDungeon.Players
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatStatsServer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerEquipmentComponent equipment;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        private PlayerCombatStatsSnapshot _snapshot;

        public PlayerCombatStatsSnapshot Snapshot => _snapshot;

        private void Awake()
        {
            _snapshot = CreateDefaultSnapshot();
        }

        private void Reset()
        {
            equipment = GetComponentInChildren<PlayerEquipmentComponent>(true);
        }

        private void OnEnable()
        {
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

        private bool IsServerActive()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        }

        private void RecomputeSnapshot(string reason)
        {
            if (!IsServerActive())
                return;

            var snapshot = CreateDefaultSnapshot();

            float hciPercentTotal = 0f;
            float dciPercentTotal = 0f;
            float diPercentTotal = 0f;
            float swingSpeedPercentTotal = 0f;

            int resistPhysical = 0;
            int resistFire = 0;
            int resistCold = 0;
            int resistPoison = 0;
            int resistEnergy = 0;

            if (equipment != null)
            {
                foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
                {
                    if (slot == EquipSlot.None)
                        continue;

                    if (!equipment.TryGetEquippedItem(slot, out var instance, out var def))
                        continue;

                    if (def != null)
                    {
                        resistPhysical += def.armor.resistPhysical;
                        resistFire += def.armor.resistFire;
                        resistCold += def.armor.resistCold;
                        resistPoison += def.armor.resistPoison;
                        resistEnergy += def.armor.resistEnergy;

                        if (slot == EquipSlot.Mainhand && def.family == ItemFamily.Mainhand)
                        {
                            snapshot.WeaponName = def.displayName;
                            snapshot.WeaponMinDamage = def.weapon.minDamage;
                            snapshot.WeaponMaxDamage = def.weapon.maxDamage;
                            snapshot.WeaponSwingSpeedSeconds = def.weapon.swingSpeedSeconds;
                            snapshot.WeaponStaminaCostPerSwing = def.weapon.staminaCostPerSwing;
                            snapshot.WeaponDamageType = (UltimateDungeon.Combat.DamageType)def.weapon.damageType;
                        }
                    }

                    if (instance != null && instance.affixes != null)
                    {
                        foreach (var affix in instance.affixes)
                        {
                            switch (affix.id)
                            {
                                case AffixId.Combat_HitChance:
                                    hciPercentTotal += affix.magnitude;
                                    break;
                                case AffixId.Combat_DefenseChance:
                                    dciPercentTotal += affix.magnitude;
                                    break;
                                case AffixId.Combat_DamageIncrease:
                                    diPercentTotal += affix.magnitude;
                                    break;
                                case AffixId.Combat_SwingSpeed:
                                    swingSpeedPercentTotal += affix.magnitude;
                                    break;
                                case AffixId.Resist_Physical:
                                    resistPhysical += Mathf.RoundToInt(affix.magnitude);
                                    break;
                                case AffixId.Resist_Fire:
                                    resistFire += Mathf.RoundToInt(affix.magnitude);
                                    break;
                                case AffixId.Resist_Cold:
                                    resistCold += Mathf.RoundToInt(affix.magnitude);
                                    break;
                                case AffixId.Resist_Poison:
                                    resistPoison += Mathf.RoundToInt(affix.magnitude);
                                    break;
                                case AffixId.Resist_Energy:
                                    resistEnergy += Mathf.RoundToInt(affix.magnitude);
                                    break;
                            }
                        }
                    }
                }
            }

            snapshot.ResistPhysical = resistPhysical;
            snapshot.ResistFire = resistFire;
            snapshot.ResistCold = resistCold;
            snapshot.ResistPoison = resistPoison;
            snapshot.ResistEnergy = resistEnergy;

            snapshot.AttackerHciPct = hciPercentTotal * 0.01f;
            snapshot.DefenderDciPct = dciPercentTotal * 0.01f;
            snapshot.DamageIncreasePct = diPercentTotal * 0.01f;

            if (Mathf.Abs(swingSpeedPercentTotal) > 0.001f)
            {
                float multiplier = Mathf.Max(0.1f, 1f - swingSpeedPercentTotal * 0.01f);
                snapshot.WeaponSwingSpeedSeconds = Mathf.Max(0.1f, snapshot.WeaponSwingSpeedSeconds * multiplier);
            }

            _snapshot = snapshot;

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[PlayerCombatStatsServer] Recomputed ({reason}) " +
                    $"weapon='{_snapshot.WeaponName}' dmg={_snapshot.WeaponMinDamage}-{_snapshot.WeaponMaxDamage} " +
                    $"swing={_snapshot.WeaponSwingSpeedSeconds:0.00}s sta={_snapshot.WeaponStaminaCostPerSwing} " +
                    $"resists=P{_snapshot.ResistPhysical} F{_snapshot.ResistFire} C{_snapshot.ResistCold} " +
                    $"Po{_snapshot.ResistPoison} E{_snapshot.ResistEnergy} " +
                    $"hci={_snapshot.AttackerHciPct:0.00} dci={_snapshot.DefenderDciPct:0.00} di={_snapshot.DamageIncreasePct:0.00}",
                    this);
            }
        }

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

        public UltimateDungeon.Combat.DamageType GetWeaponDamageType()
        {
            return _snapshot.WeaponDamageType;
        }

        public bool GetCanAttack()
        {
            return _snapshot.CanAttack;
        }

        private static PlayerCombatStatsSnapshot CreateDefaultSnapshot()
        {
            return new PlayerCombatStatsSnapshot
            {
                CanAttack = true,
                CanCast = true,
                CanMove = true,
                CanBandage = true,
                WeaponName = "Unarmed",
                WeaponMinDamage = 1,
                WeaponMaxDamage = 4,
                WeaponSwingSpeedSeconds = 2.0f,
                WeaponStaminaCostPerSwing = 0,
                WeaponDamageType = UltimateDungeon.Combat.DamageType.Physical
            };
        }
    }
}
