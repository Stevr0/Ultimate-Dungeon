using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace UltimateDungeon.Combat
{
    /// <summary>
    /// CombatResolver
    /// --------------
    /// Server-only authority that resolves completed combat actions.
    ///
    /// DESIGN LAW:
    /// - CombatResolver is the ONLY place that applies DamagePackets.
    /// - It must only ever run on the server.
    ///
    /// v0.1 assumptions:
    /// - Always hit
    /// - Fixed damage per swing
    /// - Simple death handling (despawn/destroy after a short delay)
    ///
    /// Disengage integration (COMBAT_DISENGAGE_RULES.md):
    /// - On hostile resolution (Hit/Miss/DamagePacket), refresh combat for:
    ///   - attacker
    ///   - victim
    ///
    /// Note:
    /// - Validated hostile intent refresh should happen at intent validation time
    ///   (TargetIntentValidator / AttackLegalityResolver). We do NOT do that here.
    /// </summary>
    public static class CombatResolver
    {
        // -------------------------
        // v0.1 tuning constants
        // -------------------------

        private const float DEATH_DESPAWN_DELAY_SECONDS = 0.35f;

        // Tracks victims that have already triggered death handling.
        private static readonly HashSet<ulong> _deadActors = new HashSet<ulong>();
        private static int _swingSequence;

        /// <summary>
        /// Resolve a completed weapon swing.
        /// Called by AttackLoop when a swing timer finishes.
        /// </summary>
        public static void ResolveSwing(ICombatActor attacker, ICombatActor target)
        {
            // -------------------------
            // Server safety
            // -------------------------
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                Debug.LogWarning("[CombatResolver] ResolveSwing called on non-server. Ignored.");
                return;
            }

            // -------------------------
            // Basic re-validation
            // -------------------------
            if (attacker == null || target == null)
                return;

            if (!attacker.IsAlive || !target.IsAlive)
                return;

            if (!attacker.CanAttack)
                return;

            // -------------------------
            // Combat disengage refresh (HOSTILE RESOLUTION)
            // -------------------------
            // This swing timer completion counts as a hostile resolution attempt.
            // Per COMBAT_DISENGAGE_RULES.md, BOTH attacker and victim refresh.
            RefreshCombatBoth(attacker, target);

            // -------------------------
            // v0.1 Hit resolution
            // -------------------------
            int seed = UltimateDungeon.Progression.DeterministicRng.CombineSeed(
                unchecked((int)attacker.NetId),
                unchecked((int)target.NetId),
                ++_swingSequence);
            var rng = new UltimateDungeon.Progression.DeterministicRng(seed);

            float baseHitChance = 0.5f;
            float hitChance = baseHitChance + attacker.GetAttackerHitChancePct() - target.GetDefenderDefenseChancePct();
            hitChance = Mathf.Clamp(hitChance, 0.05f, 0.95f);
            bool hit = rng.NextFloat01() <= hitChance;

            if (!hit)
            {
                // Even on miss, the hostile resolution already refreshed combat.
                // You may optionally trigger miss feedback here later.
                return;
            }

            // -------------------------
            // Build DamagePacket (v0.1)
            // -------------------------
            int minDamage = Mathf.Max(0, attacker.GetWeaponMinDamage());
            int maxDamage = Mathf.Max(minDamage, attacker.GetWeaponMaxDamage());
            int baseDamage = rng.NextInt(minDamage, maxDamage + 1);

            float damageIncrease = attacker.GetDamageIncreasePct();
            int finalDamage = Mathf.Max(0, Mathf.RoundToInt(baseDamage * (1f + damageIncrease)));
            DamageType damageType = attacker.GetWeaponDamageType();

            DamagePacket packet = DamagePacket.CreateWeaponHit(
                attacker.NetId,
                target.NetId,
                finalDamage,
                damageType,
                seed
            );

            // Apply damage (server)
            ApplyDamagePacket(packet, attacker, target);

            // Visual feedback hooks
            TryTriggerHitFeedback(target, packet.finalDamageAmount);
        }

        /// <summary>
        /// Resolve a spell damage payload.
        /// This is the canonical server path for spell-based damage.
        /// </summary>
        public static void ResolveSpellDamage(ICombatActor caster, ICombatActor target, int finalDamage, DamageType type)
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
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
                seed = 0,
                tags = DamageTags.Spell
            };

            ApplyDamagePacket(packet, caster, target);

            TryTriggerHitFeedback(target, packet.finalDamageAmount);
        }

        // -------------------------
        // Disengage helpers
        // -------------------------

        /// <summary>
        /// Refresh combat window for attacker and victim.
        ///
        /// IMPORTANT:
        /// - Only call this if the action is legal and scene-gated.
        /// - CombatResolver is downstream of legality; it assumes legality already happened.
        /// </summary>
        private static void RefreshCombatBoth(ICombatActor attacker, ICombatActor victim)
        {
            // Attacker refresh
            if (attacker.Transform != null && attacker.Transform.TryGetComponent(out CombatStateTracker atkTracker))
            {
                // Prefer the new API if present.
                atkTracker.ServerRefreshCombatWindow(victim.NetId);
            }

            // Victim refresh
            if (victim.Transform != null && victim.Transform.TryGetComponent(out CombatStateTracker vicTracker))
            {
                vicTracker.ServerRefreshCombatWindow(attacker.NetId);
            }
        }

        // -------------------------
        // Core damage application
        // -------------------------

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

            target.ApplyDamageServer(packet.finalDamageAmount, packet.damageType, attacker);

            if (!target.IsAlive)
            {
                TryTriggerDeath(victim: target, killer: attacker);
            }
        }

        // -------------------------
        // Hit feedback (optional)
        // -------------------------

        private static void TryTriggerHitFeedback(ICombatActor target, int damageAmount)
        {
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

        // -------------------------
        // Death handling (quick-fix)
        // -------------------------

        private static void TryTriggerDeath(ICombatActor victim, ICombatActor killer)
        {
            if (victim == null)
                return;

            if (!NetworkManager.Singleton.IsServer)
                return;

            if (victim.IsAlive)
                return;

            NetworkObject victimNetObj = victim.NetObject;
            if (victimNetObj == null || !victimNetObj.IsSpawned)
                return;

            ulong victimNetId = victimNetObj.NetworkObjectId;
            if (_deadActors.Contains(victimNetId))
                return;

            _deadActors.Add(victimNetId);

            Debug.Log($"[CombatResolver] Actor {victimNetId} died.");

            // -------------------------
            // SPAWN CORPSE (SPIKE)
            // -------------------------
            GameObject corpsePrefab = Resources.Load<GameObject>("PF_Corpse");
            if (corpsePrefab != null)
            {
                GameObject corpse = Object.Instantiate(
                    corpsePrefab,
                    victim.Transform.position,
                    victim.Transform.rotation
                );

                // Build a deterministic corpse-loot seed from death context instead of
                // from the corpse's own NetworkObjectId.
                //
                // Inputs:
                // - victim net id      : ties loot to the victim that died
                // - killer net id      : ties loot to who caused the death (0 if unknown)
                // - swing sequence     : deterministic event sequence already used by combat
                //
                // This keeps loot deterministic while removing dependence on spawn ordering.
                ulong killerNetId = killer != null ? killer.NetId : 0ul;
                int lootSeedInt = UltimateDungeon.Progression.DeterministicRng.CombineSeed(
                    unchecked((int)victimNetId),
                    unchecked((int)killerNetId),
                    _swingSequence);
                uint lootSeed = unchecked((uint)lootSeedInt);

                if (corpse.TryGetComponent(out CorpseLootSeedNet corpseLootSeedNet))
                {
                    // IMPORTANT: set seed immediately after Instantiate and before Spawn,
                    // so server OnNetworkSpawn consumers can use it right away.
                    corpseLootSeedNet.SetSeed(lootSeed);
                }
                else
                {
                    Debug.LogWarning("[CombatResolver] Spawned corpse is missing CorpseLootSeedNet. Loot will use fallback seeding.");
                }

                var corpseNetObj = corpse.GetComponent<NetworkObject>();
                if (corpseNetObj != null)
                    corpseNetObj.Spawn();
            }
            else
            {
                Debug.LogError("[CombatResolver] PF_Corpse not found in Resources.");
            }

            // -------------------------
            // DESPAWN MONSTER
            // -------------------------
            NetworkManager.Singleton.StartCoroutine(
                DespawnAfterDelay(victimNetObj, DEATH_DESPAWN_DELAY_SECONDS)
            );
        }


        private static IEnumerator DespawnAfterDelay(NetworkObject netObj, float delaySeconds)
        {
            if (delaySeconds > 0f)
                yield return new WaitForSeconds(delaySeconds);

            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
                yield break;

            if (netObj == null)
                yield break;

            if (!netObj.IsSpawned)
                yield break;

            netObj.Despawn(destroy: true);
        }

        // -------------------------
        // Debug helpers
        // -------------------------

        public static void ResetForDebug()
        {
            _deadActors.Clear();
        }
    }
}
