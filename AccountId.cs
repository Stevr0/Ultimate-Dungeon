// ============================================================================
// AccountId.cs — Ultimate Dungeon
// ----------------------------------------------------------------------------
// A stable, opaque identity key for an account.
//
// Why this exists:
// - Docs define AccountId as the canonical identity (planned: SteamId).
// - We must NOT use usernames / display names as identity.
// - We must NOT use NGO clientId as identity.
// - We need something stable today that survives the future Steam swap.
//
// Current MVP backend:
// - Local GUID persisted to disk (handled by LocalIdentityProvider).
//
// Future backend:
// - SteamId (string/ulong) mapped into this same AccountId type.
// ============================================================================

using System;

namespace UltimateDungeon.Networking
{
    /// <summary>
    /// AccountId
    /// ---------
    /// Canonical identity for an account in Ultimate Dungeon.
    ///
    /// Design rules (from SESSION_AND_PERSISTENCE_MODEL.md):
    /// - Planned: AccountId = SteamId
    /// - Must be stable
    /// - Must be opaque (not a username)
    /// </summary>
    [Serializable]
    public readonly struct AccountId : IEquatable<AccountId>
    {
        // We store the raw value as a string so we can support:
        // - local GUIDs today
        // - SteamId (string or ulong converted to string) later
        public readonly string Value;

        public AccountId(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Returns true if this AccountId has a non-empty value.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public override string ToString() => Value ?? string.Empty;

        // --------------------------------------------------------------------
        // Equality
        // --------------------------------------------------------------------

        public bool Equals(AccountId other)
        {
            // We treat identity as case-sensitive, because:
            // - local GUIDs are case-insensitive by nature but we store lowercase.
            // - SteamId is numeric (string compare is fine).
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is AccountId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : Value.GetHashCode();

        public static bool operator ==(AccountId a, AccountId b) => a.Equals(b);
        public static bool operator !=(AccountId a, AccountId b) => !a.Equals(b);

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------

        /// <summary>
        /// Creates a new local AccountId value.
        ///
        /// IMPORTANT:
        /// - This should only be called by the LocalIdentityProvider when there is
        ///   no persisted identity yet.
        /// - Do NOT call this per session, or players will get a new identity every launch.
        /// </summary>
        public static AccountId CreateLocalGuid()
        {
            // "local_" prefix makes it obvious this is not Steam yet.
            // We use "N" format to avoid dashes (clean for filenames/logs).
            string guid = Guid.NewGuid().ToString("N");
            return new AccountId($"local_{guid}");
        }
    }
}
