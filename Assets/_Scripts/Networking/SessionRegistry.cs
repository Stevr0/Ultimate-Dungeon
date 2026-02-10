using System.Collections.Generic;

namespace UltimateDungeon.Networking
{
    /// <summary>
    /// Server-only map of NGO clientId -> authoritative AccountId.
    /// This is populated during connection approval and consumed by server-spawned player identities.
    /// </summary>
    public static class SessionRegistry
    {
        private static readonly Dictionary<ulong, string> ClientToAccountId = new();

        public static void Register(ulong clientId, string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                return;

            ClientToAccountId[clientId] = accountId;
        }

        public static bool TryGetAccountId(ulong clientId, out string accountId)
        {
            return ClientToAccountId.TryGetValue(clientId, out accountId);
        }

        public static bool IsAccountActive(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
                return false;

            foreach (var kvp in ClientToAccountId)
            {
                if (kvp.Value == accountId)
                    return true;
            }

            return false;
        }

        public static void Remove(ulong clientId)
        {
            ClientToAccountId.Remove(clientId);
        }

        public static void Clear()
        {
            ClientToAccountId.Clear();
        }
    }
}
