using UnityEngine;

/// <summary>
/// FactionTag
/// ----------
/// Minimal identity tag for friend/foe visuals.
///
/// This is LOCAL-ONLY for now:
/// - used by UI/target ring tinting
/// - later we can make this authoritative/server-driven if needed
/// </summary>
[DisallowMultipleComponent]
public partial class FactionTag : MonoBehaviour

{
    public enum Faction
    {
        Neutral = 0,
        Friendly = 1,
        Hostile = 2
    }

    [SerializeField] private Faction faction = Faction.Neutral;

    public Faction Value => faction;
}
