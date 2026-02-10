using UnityEngine;

/// <summary>
/// FactionTag.Runtime
/// ------------------
/// Partial class extension that adds safe runtime setters WITHOUT editing your original file.
///
/// Why partial?
/// - Lets us keep your original FactionTag.cs as-is.
/// - Adds methods that can write the private serialized field because partial classes
///   share the same private members across files.
///
/// v0.1 NOTE:
/// - This is LOCAL ONLY. It changes visuals and client-side logic.
/// - If/when you make factions authoritative, we will replace this with a server-driven model.
/// </summary>
public partial class FactionTag
{
    /// <summary>
    /// Sets the faction locally.
    ///
    /// This is intended for v0.1 testing only.
    /// Later, faction hostility will be authoritative and/or derived from rule systems.
    /// </summary>
    public void SetFactionLocal(Faction newValue)
    {
        // Because this is a partial class, we can access the private field "faction"
        // declared in your original FactionTag.cs.
        faction = newValue;
    }

    /// <summary>
    /// Convenience helper.
    /// </summary>
    public void MakeHostileLocal()
    {
        SetFactionLocal(Faction.Hostile);
    }
}
