// ============================================================================
// CombatResolver.cs — Ultimate Dungeon
// ----------------------------------------------------------------------------
// Refactor Goals
// - Keep gameplay behavior the same (v0.1 assumptions), but make the code:
//   - easier to read
//   - safer (single server gate)
//   - easier to extend (clean helpers)
// - Loot: use proper server RNG so each kill can yield unique loot.
// - Preserve: corpse loot seed/table handoff via CorpseLootSeedNet.
//
// Notes
// - This is still a STATIC resolver. It depends on NetworkManager.Singleton
//   for coroutines (despawn delay).
// - In a later iteration you may move coroutine scheduling to a dedicated
//   server system (e.g., CombatSystem MonoBehaviour) so CombatResolver can be
//   pure logic.
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UltimateDungeon.Loot;
using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Combat
{
    /// <summary>
    /// CombatResolver
    /// ------------------------------------------------------------------------
    /// Server-only authority that resolves completed combat actions.
    ///
    /// DESIGN LAW:
    /// - CombatResolver is the ONLY place that applies DamagePackets.
    /// - It must only ever run on the server.
    ///
    /// v0.1 assumptions:
    /// - Hit chance is computed deterministically
    /// - Damage roll is deterministic from the swing seed
    /// - Death spawns a corpse and despawns the victim after a short delay
    ///
    /// Loot policy:
    /// - Loot seed is server RNG (unique per kill).
    /// - Everything downstream (drop tables, item instance creation, affixes)
    ///   can remain deterministic FROM that loot seed.
    /// </summary>
    public static class CombatResolver
    {
        // --------------------------------------------------------------------
        // Tunables
        // --------------------------------------------------------------------

        private const float DEATH_DESPAWN_DELAY_SECONDS = 0.35f;

        // --------------------------------------------------------------------
        // State
        // --------------------------------------------------------------------

        // Tracks victims that have already triggered death handling.
        // Prevents double-death from multiple packets in the same frame.
        private static readonly HashSet<ulong> _deadActors = new HashSet<ulong>();

        // Deterministic sequence counter used for combat resolution ordering.
        // (Not used for loot; loot uses true server RNG.)
        private static int _swingSequence;

        // --------------------------------------------------------------------
        // Public API
        // --------------------------------------------------------------------

        /// <summary>
        /// Resolve a completed weapon swing.
        /// Called by AttackLoop when a swing timer finishes.
        /// </summary>
        public static void ResolveSwing(ICombatActor attacker, ICombatActor target)
        {
            // Server-only safety gate.
            if (!IsServerActive())
            {
                Debug.LogWarning("[CombatResolver] ResolveSwing called on non-server. Ignored.");
                return;
            }

            // Basic re-validation.
            if (!IsValidAttackContext(attacker, target))
                return;

            // Combat disengage refresh (HOSTILE RESOLUTION).
            // This timer completion counts as a hostile resolution attempt.
            RefreshCombatBoth(attacker, target);

            // Deterministic swing seed (combat should be deterministic).
            int swingSeed = UltimateDungeon.Progression.DeterministicRng.CombineSeed(
                unchecked((int)attacker.NetId),
                unchecked((int)target.NetId),
                ++_swingSequence);

            var rng = new UltimateDungeon.Progression.DeterministicRng(swingSeed);

            // Resolve hit/miss.
            if (!RollHit(attacker, target, rng))
                return;

            // Roll damage.
            int finalDamage = RollDamage(attacker, rng);
            DamageType damageType = attacker.GetWeaponDamageType();

            // Build packet.
            DamagePacket packet = DamagePacket.CreateWeaponHit(
                attacker.NetId,
                target.NetId,
                finalDamage,
                damageType,
                swingSeed);

            // Apply.
            ApplyDamagePacket(packet, attacker, target);

            // Optional visuals.
            TryTriggerHitFeedback(target, packet.finalDamageAmount);
        }

        /// <summary>
        /// Resolve a spell damage payload.
        /// This is the canonical server path for spell-based damage.
        /// </summary>
        public static void ResolveSpellDamage(ICombatActor caster, ICombatActor target, int finalDamage, DamageType type)
        {
            if (!IsServerActive())
            {
                Debug.LogWarning("[CombatResolver] ResolveSpellDamage called on non-server. Ignored.");
                return;
            }

            if (caster == null || target == null)
                return;

            if (!caster.IsAlive || !target.IsAlive)
                return;

            DamagePacket packet = new DamagePacket
            {
                sourceActorNetId = caster.NetId,
                targetActorNetId = target.NetId,
                finalDamageAmount = Mathf.Max(0, finalDamage),
                damageType = type,

                // Spells can have their own seed later if needed.
                seed = 0,
                tags = DamageTags.Spell
            };

            ApplyDamagePacket(packet, caster, target);
            TryTriggerHitFeedback(target, packet.finalDamageAmount);
        }

        /// <summary>
        /// Debug helper: clears dead-actor cache so you can re-test in editor.
        /// </summary>
        public static void ResetForDebug()
        {
            _deadActors.Clear();
        }

        // --------------------------------------------------------------------
        // Validation helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// True if we have a NetworkManager and it is running as a server.
        /// </summary>
        private static bool IsServerActive()
        {
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        }

        /// <summary>
        /// Basic common validation for weapon swings.
        /// </summary>
        private static bool IsValidAttackContext(ICombatActor attacker, ICombatActor target)
        {
            if (attacker == null || target == null)
                return false;

            if (!attacker.IsAlive || !target.IsAlive)
                return false;

            if (!attacker.CanAttack)
                return false;

            return true;
        }

        // --------------------------------------------------------------------
        // Combat resolution helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Rolls hit using attacker/victim combat stats.
        /// Uses deterministic RNG passed in.
        /// </summary>
        private static bool RollHit(ICombatActor attacker, ICombatActor target, UltimateDungeon.Progression.DeterministicRng rng)
        {
            float baseHitChance = 0.5f;
            float hitChance = baseHitChance + attacker.GetAttackerHitChancePct() - target.GetDefenderDefenseChancePct();
            hitChance = Mathf.Clamp(hitChance, 0.05f, 0.95f);
            return rng.NextFloat01() <= hitChance;
        }

        /// <summary>
        /// Rolls final damage from attacker weapon damage range and damage increase.
        /// Uses deterministic RNG passed in.
        /// </summary>
        private static int RollDamage(ICombatActor attacker, UltimateDungeon.Progression.DeterministicRng rng)
        {
            int minDamage = Mathf.Max(0, attacker.GetWeaponMinDamage());
            int maxDamage = Mathf.Max(minDamage, attacker.GetWeaponMaxDamage());
            int baseDamage = rng.NextInt(minDamage, maxDamage + 1);

            float damageIncrease = attacker.GetDamageIncreasePct();
            int finalDamage = Mathf.Max(0, Mathf.RoundToInt(baseDamage * (1f + damageIncrease)));
            return finalDamage;
        }

        /// <summary>
        /// Refresh combat window for attacker and victim.
        ///
        /// IMPORTANT:
        /// - Only call this if the action is legal and scene-gated.
        /// - CombatResolver is downstream of legality; it assumes legality already happened.
        /// </summary>
        private static void RefreshCombatBoth(ICombatActor attacker, ICombatActor victim)
        {
            // Attacker refresh.
            if (attacker.Transform != null && attacker.Transform.TryGetComponent(out CombatStateTracker atkTracker))
            {
                atkTracker.ServerRefreshCombatWindow(victim.NetId);
            }

            // Victim refresh.
            if (victim.Transform != null && victim.Transform.TryGetComponent(out CombatStateTracker vicTracker))
            {
                vicTracker.ServerRefreshCombatWindow(attacker.NetId);
            }
        }

        // --------------------------------------------------------------------
        // Core damage application
        // --------------------------------------------------------------------

        /// <summary>
        /// Applies a DamagePacket to the target actor.
        /// This is the ONLY method that should call ApplyDamageServer on actors.
        /// </summary>
        private static void ApplyDamagePacket(DamagePacket packet, ICombatActor attacker, ICombatActor target)
        {
            if (packet.IsZeroDamage)
                return;

            if (target == null)
                return;

            // Gameplay-authoritative mutation happens here.
            target.ApplyDamageServer(packet.finalDamageAmount, packet.damageType, attacker);

            // Death handling.
            if (!target.IsAlive)
                TryTriggerDeath(victim: target, killer: attacker);
        }

        // --------------------------------------------------------------------
        // Hit feedback (optional)
        // --------------------------------------------------------------------

        private static void TryTriggerHitFeedback(ICombatActor target, int damageAmount)
        {
            // We only know how to flash UI/visuals if the target is a Unity Component.
            if (!(target is Component targetComponent))
                return;

            if (targetComponent.TryGetComponent(out UltimateDungeon.Visuals.HitFlash hitFlash))
            {
                hitFlash.Flash();
            }

            if (targetComponent.TryGetComponent(out DamageFeedbackReceiver feedback))
            {
                feedback.ShowDamage(damageAmount);
            }
        }

        // --------------------------------------------------------------------
        // Death handling (v0.1 / spike)
        // --------------------------------------------------------------------

        private static void TryTriggerDeath(ICombatActor victim, ICombatActor killer)
        {
            // Basic guards.
            if (victim == null)
                return;

            if (!IsServerActive())
                return;

            if (victim.IsAlive)
                return;

            NetworkObject victimNetObj = victim.NetObject;
            if (victimNetObj == null || !victimNetObj.IsSpawned)
                return;

            ulong victimNetId = victimNetObj.NetworkObjectId;

            // Prevent double execution.
            if (_deadActors.Contains(victimNetId))
                return;

            _deadActors.Add(victimNetId);
            Debug.Log($"[CombatResolver] Actor {victimNetId} died.");

            // Spawn corpse and hand off loot context (seed + optional loot table id).
            SpawnCorpseAndHandoffLootContext(victimNetObj, victim, killer);

            // Despawn victim after delay.
            NetworkManager.Singleton.StartCoroutine(DespawnAfterDelay(victimNetObj, DEATH_DESPAWN_DELAY_SECONDS));
        }

        /// <summary>
        /// Spawns PF_Corpse and hands off loot context via CorpseLootSeedNet.
        ///
        /// Loot seed policy:
        /// - Uses true server RNG so each kill can produce unique loot.
        /// - The seed is stored on the corpse and used by downstream deterministic systems
        ///   (drop tables, item instance creation, affixes).
        /// </summary>
        private static void SpawnCorpseAndHandoffLootContext(NetworkObject victimNetObj, ICombatActor victim, ICombatActor killer)
        {
            GameObject corpsePrefab = Resources.Load<GameObject>("PF_Corpse");
            if (corpsePrefab == null)
            {
                Debug.LogError("[CombatResolver] PF_Corpse not found in Resources.");
                return;
            }

            // Instantiate at the victim's current pose.
            GameObject corpse = Object.Instantiate(
                corpsePrefab,
                victim.Transform.position,
                victim.Transform.rotation);

            // "Proper RNG" seed: unique per death.
            uint lootSeed = NextServerLootSeed();

            // Optional monster-driven loot table id.
            string lootTableId = ResolveVictimLootTableId(victimNetObj.gameObject);

            // If the corpse has the seed component, hand off context.
            if (corpse.TryGetComponent(out CorpseLootSeedNet corpseLootSeedNet))
            {
                // IMPORTANT:
                // - Call these immediately after Instantiate and BEFORE Spawn().
                // - CorpseLootSeedNet buffers pre-spawn and applies in OnNetworkSpawn.
                corpseLootSeedNet.SetSeed(lootSeed);
                corpseLootSeedNet.SetLootTableId(lootTableId);
            }
            else
            {
                Debug.LogWarning("[CombatResolver] Spawned corpse is missing CorpseLootSeedNet. Loot will use fallback seeding.");
            }

            // Finally network-spawn the corpse.
            var corpseNetObj = corpse.GetComponent<NetworkObject>();
            if (corpseNetObj != null)
                corpseNetObj.Spawn();
        }

        /// <summary>
        /// Generates a server-authoritative random seed for loot.
        ///
        /// Why cryptographic RNG?
        /// - Not dependent on frame timing.
        /// - Harder to predict.
        /// - Works well for "unique items every time".
        ///
        /// Trust model note:
        /// - In Host mode, the host is the server (they can still cheat).
        /// - Dedicated server later removes that trust issue.
        /// </summary>
        private static uint NextServerLootSeed()
        {
            byte[] bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            return System.BitConverter.ToUInt32(bytes, 0);
        }

        // --------------------------------------------------------------------
        // Monster-driven loot table id handoff
        // --------------------------------------------------------------------

        /// <summary>
        /// Minimal provider contract for monster-driven corpse loot table selection.
        ///
        /// Any component on the victim that implements this interface can provide
        /// a loot table id string without coupling CombatResolver to concrete types.
        /// </summary>
        public interface ILootTableProvider
        {
            string LootTableId { get; }
        }

        /// <summary>
        /// Resolve an optional loot table id from victim components.
        ///
        /// Backward compatibility behavior:
        /// - Missing provider => empty id => corpse keeps serialized dropTable/pool.
        /// - Empty/whitespace provider value => same fallback behavior.
        /// </summary>
        private static string ResolveVictimLootTableId(GameObject victimRoot)
        {
            if (victimRoot == null)
                return string.Empty;

            var provider = victimRoot.GetComponent<ILootTableProvider>();
            if (provider == null)
                return string.Empty;

            return string.IsNullOrWhiteSpace(provider.LootTableId)
                ? string.Empty
                : provider.LootTableId.Trim();
        }

        // --------------------------------------------------------------------
        // Coroutines
        // --------------------------------------------------------------------

        private static IEnumerator DespawnAfterDelay(NetworkObject netObj, float delaySeconds)
        {
            if (delaySeconds > 0f)
                yield return new WaitForSeconds(delaySeconds);

            if (!IsServerActive())
                yield break;

            if (netObj == null)
                yield break;

            if (!netObj.IsSpawned)
                yield break;

            netObj.Despawn(destroy: true);
        }
    }
}
