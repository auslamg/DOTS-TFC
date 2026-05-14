using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Manages the full lifecycle of horde-based wave spawning, including wave progression,
/// spawn point selection, timing control, and ECS entity instantiation.
/// </summary>
/// <remarks>
/// The HordeManager acts as a singleton controller responsible for orchestrating
/// wave-based enemy spawning using predefined wave data (HordeWaveRegistrySO).
/// It supports directional and random spawn pools, wave delays, and ensures
/// reduced repetition in spawn selection.
/// </remarks>
public class HordeManager : MonoBehaviour
{
    /// <summary>
    /// Reference to the Unity ECS EntityManager used for instantiating and modifying entities.
    /// </summary>
    private EntityManager entityManager;

    /// <summary>
    /// List of spawn points located in the northern region of the map.
    /// </summary>
    [Header("Horde spawn points")]
    [SerializeField] private List<Transform> northSpawnPoints;

    /// <summary>
    /// List of spawn points located in the western region of the map.
    /// </summary>
    [SerializeField] private List<Transform> westSpawnPoints;

    /// <summary>
    /// List of spawn points located in the southern region of the map.
    /// </summary>
    [SerializeField] private List<Transform> southSpawnPoints;

    /// <summary>
    /// List of spawn points located in the eastern region of the map.
    /// </summary>
    [SerializeField] private List<Transform> eastSpawnPoints;

    /// <summary>
    /// List of spawn points used for building or lair placement.
    /// </summary>
    [Header("Building spawn points")]
    [SerializeField] private List<Transform> lairSpawnPoints;

    /// <summary>
    /// Registry containing all configured horde wave definitions.
    /// </summary>
    [Header("Waves")]
    [SerializeField] private HordeWaveRegistrySO hordeWaveRegistrySO;

    /// <summary>
    /// Initial delay (in seconds) before the first wave begins.
    /// </summary>
    [SerializeField] private float initialWaveDelay = 10f;

    /// <summary>
    /// Index of the currently active wave in the wave registry.
    /// </summary>
    public int currentWaveIndex = 0;

    /// <summary>
    /// Indicates whether the final wave condition has been reached.
    /// </summary>
    public bool finalWave = false;

    /// <summary>
    /// Timer controlling the delay between waves.
    /// </summary>
    [Header("Next Wave Timer")]
    [SerializeField] private LoopingTimer nextWaveTimer;

    /// <summary>
    /// Remaining time until the next wave begins.
    /// </summary>
    public float remainingNextWaveTime => nextWaveTimer.Time;

    /// <summary>
    /// Indicates whether the system is currently counting down to the next wave.
    /// </summary>
    public bool isCountingDownToNextWave { get; private set; }

    /// <summary>
    /// Event invoked when the final wave has completed spawning.
    /// </summary>
    public event EventHandler OnFinalWaveSpawn;

    /// <summary>
    /// Singleton instance of the HordeManager.
    /// </summary>
    public static HordeManager Instance { get; private set; }

    /// <summary>
    /// Tracks the last selected random spawn pool index to prevent immediate repetition.
    /// </summary>
    private int lastPoolIndex = -1;

    /// <summary>
    /// Tracks the last selected spawn index per spawn pool to avoid repeating the same spawn point consecutively.
    /// </summary>
    private Dictionary<List<Transform>, int> lastSpawnIndexPerPool = new();

    /// <summary>
    /// Ensures singleton integrity and prevents multiple instances of HordeManager.
    /// </summary>
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Debug.LogError("Multiple HordeManager instances found!");
            Destroy(this);
        }
    }

    /// <summary>
    /// Initializes ECS references and starts the wave loop coroutine.
    /// </summary>
    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        StartCoroutine(WaveLoop());
        UpdateWinCondition();
    }

    /// <summary>
    /// Updates whether the current wave is the final wave based on registry progress.
    /// </summary>
    private void UpdateWinCondition()
    {
        if (currentWaveIndex >= hordeWaveRegistrySO.hordeWaveSOs.Count - 1)
            finalWave = true;
    }

    /// <summary>
    /// Main coroutine controlling wave progression, including delays and transitions.
    /// </summary>
    private IEnumerator WaveLoop()
    {
        isCountingDownToNextWave = true;

        nextWaveTimer.Interval = initialWaveDelay;
        nextWaveTimer.Reset(false);

        while (!nextWaveTimer.Tick(Time.deltaTime))
            yield return null;

        isCountingDownToNextWave = false;

        while (currentWaveIndex < hordeWaveRegistrySO.hordeWaveSOs.Count)
        {
            HordeWaveSO wave = hordeWaveRegistrySO.hordeWaveSOs[currentWaveIndex];

            Debug.Log($"[Horde] Starting Wave {currentWaveIndex}");

            yield return StartCoroutine(RunWave(wave));

            currentWaveIndex++;
            UpdateWinCondition();

            if (!finalWave)
            {
                isCountingDownToNextWave = true;

                nextWaveTimer.Interval = wave.nextWaveDelay;
                nextWaveTimer.Reset(false);

                while (!nextWaveTimer.Tick(Time.deltaTime))
                    yield return null;

                isCountingDownToNextWave = false;
            }
        }

        Debug.Log("[Horde] All waves completed.");
        finalWave = true;

        OnFinalWaveSpawn?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Executes all spawn entries within a single wave sequentially.
    /// </summary>
    /// <param name="wave">The wave definition containing spawn entries.</param>
    private IEnumerator RunWave(HordeWaveSO wave)
    {
        foreach (WaveSpawnEntrySO entry in wave.spawnEntries)
        {
            yield return StartCoroutine(RunEntry(entry));
            yield return new WaitForSeconds(wave.entryInterval);
        }
    }

    /// <summary>
    /// Executes a single spawn entry, spawning multiple units over time.
    /// </summary>
    /// <param name="entry">The spawn entry defining unit type and spawn behavior.</param>
    private IEnumerator RunEntry(WaveSpawnEntrySO entry)
    {
        for (int i = 0; i < entry.spawnedAmount; i++)
        {
            SpawnUnit(entry);
            yield return new WaitForSeconds(entry.spawnInterval);
        }

        yield return new WaitForSeconds(entry.postSpawnCooldown);
    }

    /// <summary>
    /// Spawns a single unit at a selected spawn point and assigns its ECS position.
    /// </summary>
    /// <param name="entry">The spawn entry defining which unit to spawn.</param>
    private void SpawnUnit(WaveSpawnEntrySO entry)
    {
        Transform spawnPoint = GetSpawnPoint(entry.spawnDirection);

        Debug.Log($"[Horde] Spawn Direction: {entry.spawnDirection} | Position: {spawnPoint.position}");

        Entity spawnedEntity =
            entityManager.Instantiate(DataLookup.FetchEntityPrefab(EntityPrefabKey.From(entry.unitKey)));

        var localTransform = entityManager.GetComponentData<LocalTransform>(spawnedEntity);
        localTransform.Position = spawnPoint.position;
        entityManager.SetComponentData(spawnedEntity, localTransform);
    }

    /// <summary>
    /// Retrieves a spawn point based on the specified spawn direction, ensuring reduced repetition.
    /// </summary>
    /// <param name="direction">The directional spawn configuration.</param>
    /// <returns>A valid Transform representing a spawn location.</returns>
    private Transform GetSpawnPoint(WaveSpawnPoint direction)
    {
        List<Transform> pool = (direction == WaveSpawnPoint.Random)
            ? GetRandomSpawnPool()
            : GetDirectionalPool(direction);

        if (pool == null || pool.Count == 0)
        {
            Debug.LogError($"[Horde] Spawn pool empty for {direction}");
            return transform;
        }

        if (pool.Count == 1)
            return pool[0];

        if (!lastSpawnIndexPerPool.TryGetValue(pool, out int lastIndex))
            lastIndex = -1;

        int newIndex;

        do
        {
            newIndex = Random.Range(0, pool.Count);
        }
        while (newIndex == lastIndex);

        lastSpawnIndexPerPool[pool] = newIndex;

        return pool[newIndex];
    }

    /// <summary>
    /// Returns the spawn pool corresponding to a specific direction.
    /// </summary>
    /// <param name="direction">The requested spawn direction.</param>
    /// <returns>A list of transforms representing spawn points.</returns>
    private List<Transform> GetDirectionalPool(WaveSpawnPoint direction)
    {
        return direction switch
        {
            WaveSpawnPoint.North => northSpawnPoints,
            WaveSpawnPoint.West => westSpawnPoints,
            WaveSpawnPoint.South => southSpawnPoints,
            WaveSpawnPoint.East => eastSpawnPoints,
            _ => northSpawnPoints
        };
    }

    /// <summary>
    /// Randomly selects a spawn pool while avoiding immediate repetition of the previous pool.
    /// </summary>
    /// <returns>A randomly selected spawn pool.</returns>
    private List<Transform> GetRandomSpawnPool()
    {
        List<Transform>[] pools =
        {
            northSpawnPoints,
            westSpawnPoints,
            southSpawnPoints,
            eastSpawnPoints
        };

        if (pools.Length == 0)
            return northSpawnPoints;

        if (pools.Length == 1)
            return pools[0];

        int newIndex;

        do
        {
            newIndex = Random.Range(0, pools.Length);
        }
        while (newIndex == lastPoolIndex);

        lastPoolIndex = newIndex;

        var selected = pools[newIndex];

        Debug.Log($"[Horde] Random Pool Selected: {GetPoolName(selected)}");

        return selected;
    }

    /// <summary>
    /// Returns a human-readable name for a given spawn pool, primarily for debugging.
    /// </summary>
    /// <param name="pool">The spawn pool to evaluate.</param>
    /// <returns>A string representing the pool's directional identity.</returns>
    private string GetPoolName(List<Transform> pool)
    {
        if (pool == northSpawnPoints) return "North";
        if (pool == westSpawnPoints) return "West";
        if (pool == southSpawnPoints) return "South";
        if (pool == eastSpawnPoints) return "East";
        return "Unknown";
    }
}

public enum WaveSpawnPoint
{
    Random,
    North,
    West,
    South,
    East
}