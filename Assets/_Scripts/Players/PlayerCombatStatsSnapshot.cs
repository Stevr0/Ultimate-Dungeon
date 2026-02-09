using UnityEngine;
using UltimateDungeon.Combat;

namespace UltimateDungeon.Players
{
    public readonly struct WeaponDamageRange
    {
        public readonly int Min;
        public readonly int Max;

        public WeaponDamageRange(int min, int max)
        {
            Min = min;
            Max = max;
        }
    }

    public readonly struct ResistSnapshot
    {
        public readonly int Physical;
        public readonly int Fire;
        public readonly int Cold;
        public readonly int Poison;
        public readonly int Energy;

        public ResistSnapshot(int physical, int fire, int cold, int poison, int energy)
        {
            Physical = physical;
            Fire = fire;
            Cold = cold;
            Poison = poison;
            Energy = energy;
        }
    }

    public readonly struct HitChanceSnapshot
    {
        public readonly float AttackerHciPct;
        public readonly float DefenderDciPct;

        public HitChanceSnapshot(float attackerHciPct, float defenderDciPct)
        {
            AttackerHciPct = attackerHciPct;
            DefenderDciPct = defenderDciPct;
        }
    }

    public struct PlayerCombatStatsSnapshot
    {
        public string WeaponName;
        public int WeaponMinDamage;
        public int WeaponMaxDamage;
        public float WeaponSwingSpeedSeconds;
        public int WeaponStaminaCostPerSwing;
        public DamageType WeaponDamageType;

        public int ResistPhysical;
        public int ResistFire;
        public int ResistCold;
        public int ResistPoison;
        public int ResistEnergy;

        public float AttackerHciPct;
        public float DefenderDciPct;
        public float DamageIncreasePct;

        public bool CanAttack;
        public bool CanCast;
        public bool CanMove;
        public bool CanBandage;
    }
}
