using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UltimateDungeon.Actors;
using UltimateDungeon.Factions;

public class FactionDebugTest : MonoBehaviour
{
    private IEnumerator Start()
    {
        // 1) Wait until NetworkManager exists
        while (NetworkManager.Singleton == null)
            yield return null;

        // 2) Wait until Host/Client is actually started (you clicked Host)
        while (!NetworkManager.Singleton.IsListening)
            yield return null;

        // 3) Wait until NGO has created the local player object
        while (NetworkManager.Singleton.LocalClient == null ||
               NetworkManager.Singleton.LocalClient.PlayerObject == null)
            yield return null;

        // 4) Get the ActorComponent directly from the spawned player object
        var playerNo = NetworkManager.Singleton.LocalClient.PlayerObject;
        var playerActor = playerNo.GetComponent<ActorComponent>();

        if (playerActor == null)
        {
            Debug.LogWarning("[FactionTest] Local player PlayerObject has no ActorComponent.");
            yield break;
        }

        // 5) Find any other spawned actor to compare against (monster/vendor/etc.)
        var allActors = FindObjectsOfType<ActorComponent>(true);

        ActorComponent target = null;
        foreach (var a in allActors)
        {
            if (a == null || !a.IsSpawned || a == playerActor)
                continue;

            target = a;
            break;
        }

        if (target == null)
        {
            Debug.LogWarning("[FactionTest] No spawned target ActorComponent found (spawn a monster/vendor to test).");
            yield break;
        }

        var relation = FactionService.GetRelation(playerActor, target);
        Debug.Log($"[FactionTest] PlayerObject {playerActor.Type}({playerActor.Faction}) -> {target.Type}({target.Faction}) = {relation}");
    }
}
