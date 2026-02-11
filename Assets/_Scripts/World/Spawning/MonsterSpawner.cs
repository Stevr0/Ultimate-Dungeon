using System.Collections;
using System.Collections.Generic;
using UltimateDungeon.SceneRules;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Server-authoritative dungeon monster spawner for NGO.
/// 
/// Design constraints this class enforces:
/// 1) Only server/host may spawn monsters.
/// 2) Monsters only spawn when SceneRuleFlags allow hostiles (Dungeon context).
/// 3) Every spawned monster must be a NetworkObject and must call NetworkObject.Spawn().
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public sealed class MonsterSpawner : NetworkBehaviour
{
    [Header("Spawn Setup")]
    [Tooltip("Networked monster prefabs. Each entry must contain a NetworkObject component.")]
    [SerializeField] private List<NetworkObject> monsterPrefabs = new();

    [Tooltip("Optional explicit spawn points. If empty, points are auto-discovered in this scene.")]
    [SerializeField] private MonsterSpawnPoint[] spawnPoints;

    [Header("Spawn Tuning")]
    [Tooltip("Global alive cap for monsters this spawner controls.")]
    [SerializeField] private int maxAlive = 8;

    [Tooltip("Delay before initial spawn fill after network spawn.")]
    [SerializeField] private float initialSpawnDelaySeconds = 1.0f;

    [Tooltip("Delay before spawning a replacement monster after one despawns/dies.")]
    [SerializeField] private float respawnDelaySeconds = 5.0f;

    [Header("Debug")]
    [SerializeField] private bool verboseLogging;

    private readonly List<NetworkObject> _aliveMonsters = new();

    private Coroutine _spawnLoop;
    private bool _rulesResolved;
    private bool _spawnAllowedInScene;
    private float _nextReplacementSpawnAt;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Server-authoritative hard gate:
        // Clients are never allowed to execute spawning logic.
        if (!IsServer)
            return;

        // Defensive: if this object is re-spawned/re-enabled due to scene reload,
        // stop an older loop before starting a new one so we never double-spawn.
        if (_spawnLoop != null)
        {
            StopCoroutine(_spawnLoop);
            _spawnLoop = null;
        }

        _aliveMonsters.Clear();
        DiscoverSpawnPointsIfNeeded();

        _spawnLoop = StartCoroutine(ServerSpawnLoop());
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && _spawnLoop != null)
        {
            StopCoroutine(_spawnLoop);
            _spawnLoop = null;
        }

        _aliveMonsters.Clear();
        _rulesResolved = false;
        _spawnAllowedInScene = false;

        base.OnNetworkDespawn();
    }

    private IEnumerator ServerSpawnLoop()
    {
        // Wait for scene rules to become available.
        // In additive loading, provider spawn order can vary.
        while (!_rulesResolved)
        {
            ResolveSceneRuleGate();

            if (!_rulesResolved)
                yield return null;
        }

        // Hard gate from ACTOR_MODEL/COMBAT_CORE: no hostiles in safe scenes.
        if (!_spawnAllowedInScene)
        {
            if (verboseLogging)
            {
                Debug.Log($"[MonsterSpawner] Scene '{gameObject.scene.name}' does not allow hostiles. Spawner remains idle.", this);
            }

            yield break;
        }

        if (initialSpawnDelaySeconds > 0f)
            yield return new WaitForSeconds(initialSpawnDelaySeconds);

        // Initial fill: spawn up to maxAlive right away once allowed.
        SpawnToCapServer();
        _nextReplacementSpawnAt = Time.time + Mathf.Max(0f, respawnDelaySeconds);

        while (IsServer && IsSpawned)
        {
            PruneDespawnedTrackedMonsters();

            // Safety: re-check scene gate during runtime. If rule context changes to safe,
            // we do not spawn replacements.
            if (!IsHostileSpawnAllowedForCurrentScene())
            {
                yield return null;
                continue;
            }

            if (_aliveMonsters.Count < Mathf.Max(0, maxAlive) && Time.time >= _nextReplacementSpawnAt)
            {
                TrySpawnOneServer();
                _nextReplacementSpawnAt = Time.time + Mathf.Max(0f, respawnDelaySeconds);
            }

            yield return null;
        }
    }

    private void ResolveSceneRuleGate()
    {
        _spawnAllowedInScene = IsHostileSpawnAllowedForCurrentScene();

        // Mark resolved if we found a provider snapshot for this scene OR a global registry snapshot.
        // If neither is available yet, additive load order likely hasn't registered the provider.
        _rulesResolved = TryGetSceneFlagsForSpawnerScene(out _);
    }

    private bool IsHostileSpawnAllowedForCurrentScene()
    {
        if (!TryGetSceneFlagsForSpawnerScene(out SceneRuleFlags flags))
            return false;

        return flags.HasFlag(SceneRuleFlags.HostileActorsAllowed);
    }

    private bool TryGetSceneFlagsForSpawnerScene(out SceneRuleFlags flags)
    {
        Scene scene = gameObject.scene;

        // Preferred source: the SceneRuleProvider in this exact scene.
        SceneRuleProvider[] providers = FindObjectsByType<SceneRuleProvider>(FindObjectsSortMode.None);
        for (int i = 0; i < providers.Length; i++)
        {
            SceneRuleProvider provider = providers[i];
            if (provider == null || provider.gameObject.scene != scene)
                continue;

            SceneRuleSnapshot snapshot = provider.GetSnapshot();
            if (!snapshot.IsValid)
                continue;

            flags = snapshot.Flags;
            return true;
        }

        // Fallback: global registry snapshot (used throughout the project).
        if (SceneRuleRegistry.HasCurrent)
        {
            flags = SceneRuleRegistry.Current.Flags;
            return true;
        }

        flags = SceneRuleFlags.None;
        return false;
    }

    private void DiscoverSpawnPointsIfNeeded()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
            return;

        List<MonsterSpawnPoint> found = new();
        GameObject[] roots = gameObject.scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null)
                continue;

            found.AddRange(roots[i].GetComponentsInChildren<MonsterSpawnPoint>(includeInactive: false));
        }

        spawnPoints = found.ToArray();

        if (verboseLogging)
            Debug.Log($"[MonsterSpawner] Auto-discovered {spawnPoints.Length} spawn points in scene '{gameObject.scene.name}'.", this);
    }

    private void SpawnToCapServer()
    {
        PruneDespawnedTrackedMonsters();

        int targetCap = Mathf.Max(0, maxAlive);
        while (_aliveMonsters.Count < targetCap)
        {
            if (!TrySpawnOneServer())
                break;
        }
    }

    private bool TrySpawnOneServer()
    {
        if (!IsServer)
            return false;

        if (monsterPrefabs == null || monsterPrefabs.Count == 0)
        {
            Debug.LogWarning("[MonsterSpawner] No monster prefabs configured. Cannot spawn.", this);
            return false;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[MonsterSpawner] No spawn points configured/found. Cannot spawn.", this);
            return false;
        }

        NetworkObject prefab = PickRandomPrefab();
        MonsterSpawnPoint point = PickRandomSpawnPoint();

        if (prefab == null || point == null)
            return false;

        // NGO correctness:
        // Instantiate on server, then call Spawn() so all clients receive the object.
        NetworkObject instance = Instantiate(prefab, point.SpawnTransform.position, point.SpawnTransform.rotation);
        if (instance == null)
            return false;

        if (!instance.TryGetComponent<NetworkObject>(out NetworkObject instanceNetworkObject) || instanceNetworkObject == null)
        {
            Debug.LogWarning($"[MonsterSpawner] Spawned prefab '{prefab.name}' without NetworkObject instance. Destroying instance.", this);
            Destroy(instance.gameObject);
            return false;
        }

        instanceNetworkObject.Spawn();
        _aliveMonsters.Add(instanceNetworkObject);

        if (verboseLogging)
            Debug.Log($"[MonsterSpawner] Spawned '{instanceNetworkObject.name}' at '{point.name}'. Alive={_aliveMonsters.Count}/{maxAlive}", this);

        return true;
    }

    private void PruneDespawnedTrackedMonsters()
    {
        for (int i = _aliveMonsters.Count - 1; i >= 0; i--)
        {
            NetworkObject tracked = _aliveMonsters[i];
            if (tracked == null || !tracked.IsSpawned)
                _aliveMonsters.RemoveAt(i);
        }
    }

    private NetworkObject PickRandomPrefab()
    {
        int count = monsterPrefabs.Count;
        if (count == 0)
            return null;

        int startIndex = Random.Range(0, count);
        for (int i = 0; i < count; i++)
        {
            NetworkObject prefab = monsterPrefabs[(startIndex + i) % count];
            if (prefab == null)
                continue;

            if (!prefab.TryGetComponent<NetworkObject>(out _))
            {
                Debug.LogWarning($"[MonsterSpawner] Prefab '{prefab.name}' is missing NetworkObject. Skipping.", this);
                continue;
            }

            return prefab;
        }

        return null;
    }

    private MonsterSpawnPoint PickRandomSpawnPoint()
    {
        int count = spawnPoints.Length;
        if (count == 0)
            return null;

        int startIndex = Random.Range(0, count);
        for (int i = 0; i < count; i++)
        {
            MonsterSpawnPoint point = spawnPoints[(startIndex + i) % count];
            if (point != null)
                return point;
        }

        return null;
    }
}
