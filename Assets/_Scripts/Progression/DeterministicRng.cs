// ============================================================================
// DeterministicRng.cs
// ----------------------------------------------------------------------------
// Small, deterministic RNG wrapper for server-side rolls.
//
// Why not UnityEngine.Random?
// - UnityEngine.Random is global state and can be affected by unrelated code.
// - We want rolls that are repeatable given the same seed.
//
// This wrapper uses System.Random with an explicit seed.
// ============================================================================


using System;


namespace UltimateDungeon.Progression
{
    /// <summary>
    /// DeterministicRng
    /// ----------------
    /// A minimal RNG that produces repeatable results given a seed.
    ///
    /// IMPORTANT:
    /// - Always create a new instance per event using a stable seed.
    /// - Do NOT share instances across unrelated systems.
    /// </summary>
    public struct DeterministicRng
    {
        private System.Random _rand;


        public DeterministicRng(int seed)
        {
            _rand = new System.Random(seed);
        }


        /// <summary>
        /// Returns a float in [0, 1).
        /// </summary>
        public float NextFloat01()
        {
            // System.Random.NextDouble returns [0,1)
            return (float)_rand.NextDouble();
        }


        /// <summary>
        /// Returns an int in [min, maxExclusive).
        /// </summary>
        public int NextInt(int min, int maxExclusive)
        {
            return _rand.Next(min, maxExclusive);
        }


        /// <summary>
        /// Simple hash combiner for building seeds deterministically.
        ///
        /// Using a stable mixing function avoids trivial collisions.
        /// </summary>
        public static int CombineSeed(params int[] values)
        {
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < values.Length; i++)
                {
                    hash = hash * 31 + values[i];
                }
                return hash;
            }
        }
    }
}