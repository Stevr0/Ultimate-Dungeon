using Unity.Netcode;
using UnityEngine;

/// <summary>
/// DamageFeedbackReceiver
/// ----------------------
/// Client-only visual feedback for damage.
/// </summary>
public class DamageFeedbackReceiver : NetworkBehaviour
{
    [SerializeField] private FloatingDamageText floatingTextPrefab;
    [SerializeField] private Vector3 offset = new Vector3(0, 1.8f, 0);

    /// <summary>
    /// Called by CombatResolver via ClientRpc
    /// </summary>
    public void ShowDamage(int amount)
    {
        if (!IsClient)
            return;

        var text = Instantiate(
            floatingTextPrefab,
            transform.position + offset,
            Quaternion.identity
        );

        text.Initialize(amount);
    }
}
