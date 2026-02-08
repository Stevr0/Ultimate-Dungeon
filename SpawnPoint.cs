// ============================================================================
// SpawnPoint.cs � Ultimate Dungeon (MVP)
// ----------------------------------------------------------------------------
// PURPOSE
// - Provides an authored spawn transform inside a gameplay scene (Village/Dungeon/etc.).
// - SceneTransitionService searches for a SpawnPoint with a matching Tag.
// - If none exists, the service will fall back to "Default" then Vector3.zero.
//
// HOW TO USE
// 1) In a gameplay scene (e.g., SCN_Village), create an empty GameObject.
// 2) Name it something like: SP_Default (or SP_Entrance).
// 3) Add this SpawnPoint component.
// 4) Set Tag to match what your SceneTransitionService uses:
//      - InitialSpawnTag (for initial load)
//      - DestinationSpawnTag (from ScenePortal)
//
// NOTES
// - This is NOT a NetworkBehaviour and does NOT need a NetworkObject.
// - It is pure scene auth data.
// ============================================================================

using UnityEngine;

namespace UltimateDungeon.Scenes
{
    [DisallowMultipleComponent]
    public sealed class SpawnPoint : MonoBehaviour
    {
        [Tooltip("Logical spawn tag used by SceneTransitionService (e.g. Default, Entrance, Exit).")]
        public string Tag = "Default";

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw a small marker so you can see spawn points in the Scene view.
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.35f);

            // Draw a label with the tag for clarity.
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"Spawn: {Tag}");
        }
#endif
    }
}
