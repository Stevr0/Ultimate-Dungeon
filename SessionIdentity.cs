// ============================================================================
// SessionIdentity.cs — Ultimate Dungeon
// ----------------------------------------------------------------------------
// Represents the authenticated identity for THIS runtime session.
//
// This is intentionally tiny:
// - AccountId (canonical, stable)
// - DisplayName (cosmetic)
// - BuildId (optional gating)
// - IsHost (useful for UI/debug)
//
// IMPORTANT:
// - This does NOT include Character data.
// - Character Snapshot transport is a separate step in the join flow.
// ============================================================================

using System;

namespace UltimateDungeon.Networking
{
    [Serializable]
    public struct SessionIdentity
    {
        public AccountId accountId;
        public string displayName;
        public string buildId;
        public bool isHost;

        public bool IsValid => accountId.IsValid;

        public override string ToString()
        {
            string name = string.IsNullOrWhiteSpace(displayName) ? "<no-name>" : displayName;
            return $"{name} ({accountId})";
        }
    }
}
