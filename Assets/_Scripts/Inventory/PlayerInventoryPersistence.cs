using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UltimateDungeon.Items
{
    public static class PlayerInventoryPersistence
    {
        private const string SavesRoot = "Saves/Accounts";

        public static PlayerInventorySaveData BuildSaveData(string accountId, ulong ownerClientId, InventoryRuntimeModel runtime)
        {
            var data = new PlayerInventorySaveData
            {
                accountId = accountId,
                characterId = $"char_{ownerClientId}",
                characterName = $"Player_{ownerClientId}",
                createdAtUtc = DateTime.UtcNow.ToString("O"),
                lastSeenAtUtc = DateTime.UtcNow.ToString("O"),
                inventorySlots = runtime?.SlotCount ?? 0,
                inventoryItems = new List<SavedInventoryEntry>()
            };

            if (runtime == null)
                return data;

            for (int i = 0; i < runtime.SlotCount; i++)
            {
                var slot = runtime.GetSlot(i);
                if (slot.IsEmpty || slot.item == null)
                    continue;

                data.inventoryItems.Add(new SavedInventoryEntry
                {
                    slotIndex = i,
                    item = SavedItemInstance.FromRuntime(slot.item)
                });
            }

            return data;
        }

        public static InventoryRuntimeModel BuildRuntimeModel(PlayerInventorySaveData save, int defaultSlotCount, ItemDefCatalog catalog)
        {
            int slots = Mathf.Max(defaultSlotCount, save?.inventorySlots ?? defaultSlotCount);
            var model = new InventoryRuntimeModel(slots);

            if (save?.inventoryItems == null)
                return model;

            for (int i = 0; i < save.inventoryItems.Count; i++)
            {
                var entry = save.inventoryItems[i];
                if (entry == null || entry.item == null)
                    continue;

                var item = entry.item.ToRuntime();
                if (item == null)
                    continue;

                if (entry.slotIndex >= 0 && entry.slotIndex < model.SlotCount)
                {
                    model.TryPlaceIntoEmptySlot(entry.slotIndex, item, catalog);
                }
                else
                {
                    model.TryAdd(item, catalog, out _);
                }
            }

            return model;
        }

        public static bool TryLoad(string accountId, out PlayerInventorySaveData save)
        {
            save = null;
            string path = GetCharacterPath(accountId);
            if (!File.Exists(path))
                return false;

            try
            {
                string json = File.ReadAllText(path);
                save = JsonUtility.FromJson<PlayerInventorySaveData>(json);
                return save != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PlayerInventoryPersistence] Failed to load '{path}': {ex.Message}");
                return false;
            }
        }

        public static void Save(PlayerInventorySaveData save)
        {
            if (save == null || string.IsNullOrWhiteSpace(save.accountId))
                return;

            string dir = GetAccountDirectory(save.accountId);
            Directory.CreateDirectory(dir);

            string path = GetCharacterPath(save.accountId);
            string tmp = path + ".tmp";

            try
            {
                string json = JsonUtility.ToJson(save, prettyPrint: true);
                File.WriteAllText(tmp, json);
                File.Copy(tmp, path, overwrite: true);
                File.Delete(tmp);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerInventoryPersistence] Failed to save '{path}': {ex.Message}");
            }
        }

        private static string GetAccountDirectory(string accountId)
        {
            return Path.Combine(Application.persistentDataPath, SavesRoot, accountId);
        }

        private static string GetCharacterPath(string accountId)
        {
            return Path.Combine(GetAccountDirectory(accountId), "character.json");
        }
    }

    [Serializable]
    public sealed class PlayerInventorySaveData
    {
        public string accountId;
        public string characterId;
        public string characterName;
        public string createdAtUtc;
        public string lastSeenAtUtc;
        public int inventorySlots;
        public List<SavedInventoryEntry> inventoryItems;
    }

    [Serializable]
    public sealed class SavedInventoryEntry
    {
        public int slotIndex;
        public SavedItemInstance item;
    }

    [Serializable]
    public sealed class SavedItemInstance
    {
        public string itemDefId;
        public string instanceId;
        public int stackCount;
        public float durabilityCurrent;
        public float durabilityMax;
        public int activeGrantSlot;
        public List<SavedAffixInstance> affixes;
        public List<SavedGrantedAbilitySelection> grantedAbilitySelections;

        public static SavedItemInstance FromRuntime(ItemInstance item)
        {
            var data = new SavedItemInstance
            {
                itemDefId = item.itemDefId,
                instanceId = item.instanceId,
                stackCount = item.stackCount,
                durabilityCurrent = item.durabilityCurrent,
                durabilityMax = item.durabilityMax,
                activeGrantSlot = (int)item.activeGrantSlot,
                affixes = new List<SavedAffixInstance>(),
                grantedAbilitySelections = new List<SavedGrantedAbilitySelection>()
            };

            if (item.affixes != null)
            {
                for (int i = 0; i < item.affixes.Count; i++)
                {
                    data.affixes.Add(new SavedAffixInstance
                    {
                        id = item.affixes[i].id.ToString(),
                        magnitude = item.affixes[i].magnitude
                    });
                }
            }

            if (item.grantedAbilitySelections != null)
            {
                for (int i = 0; i < item.grantedAbilitySelections.Count; i++)
                {
                    data.grantedAbilitySelections.Add(new SavedGrantedAbilitySelection
                    {
                        slot = (int)item.grantedAbilitySelections[i].slot,
                        spellId = item.grantedAbilitySelections[i].spellId.ToString()
                    });
                }
            }

            return data;
        }

        public ItemInstance ToRuntime()
        {
            if (string.IsNullOrWhiteSpace(itemDefId))
                return null;

            var runtime = new ItemInstance(itemDefId)
            {
                instanceId = instanceId,
                stackCount = Mathf.Max(1, stackCount),
                durabilityCurrent = durabilityCurrent,
                durabilityMax = durabilityMax,
                activeGrantSlot = (AbilityGrantSlot)Mathf.Max(0, activeGrantSlot),
                affixes = new List<AffixInstance>(),
                grantedAbilitySelections = new List<GrantedAbilitySelection>()
            };

            if (affixes != null)
            {
                for (int i = 0; i < affixes.Count; i++)
                {
                    if (!Enum.TryParse(affixes[i].id, out AffixId parsedAffix))
                        continue;

                    runtime.affixes.Add(new AffixInstance(parsedAffix, affixes[i].magnitude));
                }
            }

            if (grantedAbilitySelections != null)
            {
                for (int i = 0; i < grantedAbilitySelections.Count; i++)
                {
                    if (!Enum.TryParse(grantedAbilitySelections[i].spellId, out UltimateDungeon.Spells.SpellId parsedSpell))
                        parsedSpell = UltimateDungeon.Spells.SpellId.None;

                    runtime.grantedAbilitySelections.Add(new GrantedAbilitySelection
                    {
                        slot = (AbilityGrantSlot)Mathf.Max(0, grantedAbilitySelections[i].slot),
                        spellId = parsedSpell
                    });
                }
            }

            runtime.EnsureInstanceId();
            return runtime;
        }
    }

    [Serializable]
    public sealed class SavedAffixInstance
    {
        public string id;
        public float magnitude;
    }

    [Serializable]
    public sealed class SavedGrantedAbilitySelection
    {
        public int slot;
        public string spellId;
    }
}
