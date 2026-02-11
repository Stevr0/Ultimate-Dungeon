// ============================================================================
// LocalIdentityProvider.cs — Ultimate Dungeon
// ----------------------------------------------------------------------------
// MVP identity backend.
//
// What it does:
// - Creates a stable local AccountId (GUID) ONCE, then persists it to disk.
// - Returns the same AccountId on every launch.
//
// Why:
// - SESSION_AND_PERSISTENCE_MODEL.md locks AccountId as the identity key.
// - SteamId will replace this provider later without changing gameplay code.
//
// Save location (recommended by SESSION_AND_PERSISTENCE_MODEL.md):
//   Saves/Accounts/<AccountId>/...
// For the LOCAL provider we store a single "local_identity.json" file so we
// can discover the AccountId before we know the folder name.
// ============================================================================

using System;
using System.IO;
using UnityEngine;

namespace UltimateDungeon.Networking
{
    /// <summary>
    /// Abstraction layer for account identity.
    ///
    /// Today: LocalIdentityProvider (GUID)
    /// Future: SteamIdentityProvider (SteamId)
    /// </summary>
    public interface IIdentityProvider
    {
        AccountId GetAccountId();
        string GetDisplayName();

        /// <summary>
        /// Optional. Useful for local testing.
        /// Steam provider may ignore this or mirror Steam persona name.
        /// </summary>
        void SetDisplayName(string newName);
    }

    /// <summary>
    /// Persists a local GUID identity.
    /// Attach via IdentityBootstrapper.
    /// </summary>
    public sealed class LocalIdentityProvider : IIdentityProvider
    {
        // --------------------------------------------------------------------
        // Save file
        // --------------------------------------------------------------------

        [Serializable]
        private struct LocalIdentityRecord
        {
            public string accountId;
            public string displayName;
        }

        private readonly string _filePath;
        private LocalIdentityRecord _record;
        private bool _loaded;

        public LocalIdentityProvider()
        {
            // Application.persistentDataPath is the correct Unity location for saves.
            // We keep a simple predictable file for the "who am I" record.
            string root = Path.Combine(Application.persistentDataPath, "Saves", "Accounts");
            _filePath = Path.Combine(root, "local_identity.json");
        }

        public AccountId GetAccountId()
        {
            EnsureLoaded();
            return new AccountId(_record.accountId);
        }

        public string GetDisplayName()
        {
            EnsureLoaded();
            return string.IsNullOrWhiteSpace(_record.displayName) ? "Player" : _record.displayName;
        }

        public void SetDisplayName(string newName)
        {
            EnsureLoaded();
            _record.displayName = string.IsNullOrWhiteSpace(newName) ? "Player" : newName.Trim();
            Save();
        }

        // --------------------------------------------------------------------
        // Load / Save
        // --------------------------------------------------------------------

        private void EnsureLoaded()
        {
            if (_loaded)
                return;

            _loaded = true;

            try
            {
                string dir = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    _record = JsonUtility.FromJson<LocalIdentityRecord>(json);
                }

                // If missing or corrupted, create a new record.
                if (string.IsNullOrWhiteSpace(_record.accountId))
                {
                    AccountId id = AccountId.CreateLocalGuid();
                    _record.accountId = id.Value;
                }

                if (string.IsNullOrWhiteSpace(_record.displayName))
                    _record.displayName = "Player";

                Save();
            }
            catch (Exception e)
            {
                // If we cannot load/save, we still want to fail in a visible way.
                Debug.LogError($"[LocalIdentityProvider] Failed to load identity: {e}");

                // Best-effort fallback: session-only identity (NOT persisted).
                // This should be rare; it indicates filesystem permission issues.
                AccountId id = AccountId.CreateLocalGuid();
                _record.accountId = id.Value;
                _record.displayName = "Player";
            }
        }

        private void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonUtility.ToJson(_record, prettyPrint: true);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[LocalIdentityProvider] Failed to save identity: {e}");
            }
        }
    }
}
