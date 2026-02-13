using UltimateDungeon.Combat;
using UnityEngine;

/// <summary>
/// Simple monster-side loot profile.
///
/// Purpose:
/// - Expose a designer-editable loot table id on a monster prefab/instance.
/// - Provide that id through <see cref="CombatResolver.ILootTableProvider"/> so combat death handling
///   can hand the value off to corpse loot resolution without coupling to this concrete component.
///
/// Design notes:
/// - Empty string is intentionally valid. It means "no explicit override" and allows downstream fallback
///   behavior (for example, corpse defaults).
/// - Validation is intentionally lightweight and editor-focused.
/// - Any validation logs are optional and controlled by a debug toggle so normal workflows stay quiet.
/// </summary>
[DisallowMultipleComponent]
public sealed class MonsterLootProfile : MonoBehaviour, CombatResolver.ILootTableProvider
{
    [Header("Loot Table Provider")]
    [Tooltip("Optional loot table id for this monster. Leave empty to use default/fallback loot behavior.")]
    [SerializeField] private string lootTableId = string.Empty;

    [Header("Debug")]
    [Tooltip("When enabled, validation emits informational logs in the editor.")]
    [SerializeField] private bool debugLogs;

    /// <summary>
    /// Contract implementation for <see cref="CombatResolver.ILootTableProvider"/>.
    ///
    /// We return the serialized field directly so the provider remains simple and predictable.
    /// Empty is allowed and meaningful (fallback behavior).
    /// </summary>
    public string LootTableId => lootTableId;

    /// <summary>
    /// Basic editor-time validation.
    ///
    /// Current rules:
    /// - Null is normalized to empty to avoid propagating null strings at runtime.
    /// - Optional cosmetic trim for leading/trailing whitespace.
    /// - Empty result is still valid and not treated as an error.
    ///
    /// Logging behavior:
    /// - Logs only when <see cref="debugLogs"/> is enabled.
    /// - Uses informational logs (not warnings/errors) because empty values are acceptable.
    /// </summary>
    private void OnValidate()
    {
        // Normalize null to empty so runtime consumers can safely treat this as a plain string.
        if (lootTableId == null)
        {
            lootTableId = string.Empty;

            if (debugLogs)
                Debug.Log("[MonsterLootProfile] lootTableId was null and has been normalized to empty.", this);

            return;
        }

        // Keep authoring tidy by trimming accidental spaces; empty-after-trim is still valid.
        string trimmed = lootTableId.Trim();
        if (!string.Equals(trimmed, lootTableId, System.StringComparison.Ordinal))
        {
            lootTableId = trimmed;

            if (debugLogs)
                Debug.Log("[MonsterLootProfile] Trimmed surrounding whitespace from lootTableId.", this);
        }
        else if (debugLogs && lootTableId.Length == 0)
        {
            // Optional debug signal so designers can confirm intentional fallback configuration.
            Debug.Log("[MonsterLootProfile] lootTableId is empty. Fallback loot behavior will be used.", this);
        }
    }
}
