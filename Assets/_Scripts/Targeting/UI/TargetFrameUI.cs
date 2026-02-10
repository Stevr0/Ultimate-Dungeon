using TMPro;
using UnityEngine;
using UltimateDungeon.Players;
using UltimateDungeon.Players.Networking;

/// <summary>
/// TargetFrameUI
/// ------------
/// Minimal UI for UO-style targeting.
///
/// - Shows the local player's current target name
/// - Local-only (not networked)
///
/// How it finds the local player:
/// - Uses PlayerNetIdentity.Local (set when local player spawns)
/// - Reads PlayerTargeting from the local player
///
/// Why polling is OK for now:
/// - Bootstrapping phase
/// - We'll swap to events once we lock the data flow
/// </summary>
public class TargetFrameUI : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;

    [Header("Format")]
    [SerializeField] private string prefix = "Target: ";

    private PlayerTargeting _targeting;

    private void OnEnable()
    {
        TryBind();
        Refresh();
    }

    private void Update()
    {
        // If we aren't bound yet (e.g., UI enabled before player spawns), keep trying.
        if (_targeting == null)
        {
            TryBind();
            return;
        }

        Refresh();
    }

    private void TryBind()
    {
        // PlayerNetIdentity.Local is set by your existing networking bootstrap.
        if (PlayerNetIdentity.Local == null)
            return;

        _targeting = PlayerNetIdentity.Local.GetComponent<PlayerTargeting>();
        if (_targeting == null)
            Debug.LogWarning("[TargetFrameUI] Local player has no PlayerTargeting.");
    }

    private void Refresh()
    {
        if (targetText == null)
            return;

        string name = "None";

        if (_targeting != null && _targeting.CurrentTarget != null)
            name = _targeting.CurrentTarget.name;

        targetText.text = prefix + name;
    }
}
