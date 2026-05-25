using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;
using Dto;

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
    /// ECS EntityManager used for instantiating and modifying entities at runtime.
    /// </summary>
    private EntityManager entityManager;

    /// <summary>
    /// Spawn points located in the northern region of the map.
    /// </summary>
    [Header("Horde spawn points")]
    [SerializeField] private List<Transform> northSpawnPoints;

    /// <summary>
    /// Spawn points located in the western region of the map.
    /// </summary>
    [SerializeField] private List<Transform> westSpawnPoints;

    /// <summary>
    /// Spawn points located in the southern region of the map.
    /// </summary>
    [SerializeField] private List<Transform> southSpawnPoints;

    /// <summary>
    /// Spawn points located in the eastern region of the map.
    /// </summary>
    [SerializeField] private List<Transform> eastSpawnPoints;

    /// <summary>
    /// Spawn points used for lair or building placement.
    /// </summary>
    [Header("Building spawn points")]
    [SerializeField] private List<Transform> lairSpawnPoints;

    /// <summary>
    /// Registry containing all wave definitions used by the horde system.
    /// </summary>
    [Header("Waves")]
    [SerializeField] private HordeWaveRegistrySO hordeWaveRegistrySO;

    /// <summary>
    /// Initial delay before the first wave begins.
    /// </summary>
    [SerializeField] private float initialWaveDelay = 10f;

    /// <summary>
    /// Current wave index in the wave registry.
    /// </summary>
    public int currentWaveIndex = 0;

    /// <summary>
    /// Indicates whether the final wave has been reached.
    /// </summary>
    public bool finalWave = false;

    /// <summary>
    /// Indicates whether the system is currently counting down to the next wave.
    /// </summary>
    public bool isCountingDownToNextWave => currentState == HordeState.WaitingForWaveStart && HasRemainingWaves();

    /// <summary>
    /// Remaining time until the next wave starts.
    /// </summary>
    public float remainingNextWaveTime => isCountingDownToNextWave ? stateTimer.Time : 0f;

    /// <summary>
    /// Exposes the interval used by the current timer state.
    /// </summary>
    public float nextWaveInterval => stateTimer.Interval;

    /// <summary>
    /// Invoked when the final wave has completed spawning.
    /// </summary>
    public event EventHandler OnFinalWaveSpawn;

    /// <summary>
    /// Singleton instance of the HordeManager.
    /// </summary>
    public static HordeManager Instance { get; private set; }

    /// <summary>
    /// Tracks last selected random spawn pool index to avoid repetition.
    /// </summary>
    private int lastPoolIndex = -1;

    /// <summary>
    /// Exposes the last random pool index used for spawning.
    /// </summary>
    public int LastPoolIndex => lastPoolIndex;

    /// <summary>
    /// Tracks last selected spawn index per pool to avoid repeating spawn points.
    /// </summary>
    private Dictionary<List<Transform>, int> lastSpawnIndexPerPool = new();

    // --- Runtime progress tracking (persisted in managed save data) ---
    /// <summary>
    /// Index of the currently active spawn entry within the current wave (-1 when none).
    /// </summary>
    public int currentSpawnEntryIndex = -1;

    /// <summary>
    /// How many units have been spawned so far for the active spawn entry.
    /// </summary>
    public int currentSpawnedInEntry = 0;

    /// <summary>
    /// Remaining time until the next unit spawn in the current entry.
    /// </summary>
    public float spawnEntryRemainingInterval = 0f;

    /// <summary>
    /// Remaining post-spawn cooldown for the current entry.
    /// </summary>
    public float spawnEntryPostCooldownRemaining = 0f;

    private HordeState currentState = HordeState.Uninitialized;
    private LoopingTimer stateTimer = new();
    public int currentEntryIndex = -1;

    /// <summary>
    /// Ensures singleton instance integrity.
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
    /// Initializes ECS references and starts the wave progression loop.
    /// </summary>
    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (currentState == HordeState.Uninitialized)
            BeginWaveCountdown(initialWaveDelay);

        UpdateWinCondition();
    }

    /// <summary>
    /// Updates whether the final wave condition has been reached.
    /// </summary>
    private void UpdateWinCondition()
    {
        if (hordeWaveRegistrySO == null || hordeWaveRegistrySO.hordeWaveSOs.Count == 0)
        {
            finalWave = true;
            return;
        }

        finalWave = currentWaveIndex >= hordeWaveRegistrySO.hordeWaveSOs.Count - 1;
    }

    /// <summary>
    /// Unity update loop for the horde state machine.
    /// </summary>
    private void Update()
    {
        if (currentState == HordeState.Uninitialized || currentState == HordeState.Completed)
            return;

        if (stateTimer.Tick(Time.deltaTime))
        {
            AdvanceState();
        }

        UpdateDebugIntervals();
    }

    /// <summary>
    /// Updates the exposed timer fields used by save state and UI.
    /// </summary>
    private void UpdateDebugIntervals()
    {
        spawnEntryRemainingInterval = currentState == HordeState.SpawningEntry ? stateTimer.Time : 0f;
        spawnEntryPostCooldownRemaining = currentState == HordeState.WaitingForPostEntryCooldown ? stateTimer.Time : 0f;
    }

    /// <summary>
    /// Begins waiting for the next wave to start.
    /// </summary>
    /// <param name="delay">Delay before the wave begins.</param>
    private void BeginWaveCountdown(float delay)
    {
        currentState = HordeState.WaitingForWaveStart;
        stateTimer = new LoopingTimer { Time = delay, Interval = delay };
        currentEntryIndex = -1;
        currentSpawnEntryIndex = -1;
        currentSpawnedInEntry = 0;
        spawnEntryRemainingInterval = 0f;
        spawnEntryPostCooldownRemaining = 0f;
    }

    /// <summary>
    /// Drives the state machine when the current timer reaches zero.
    /// </summary>
    private void AdvanceState()
    {
        switch (currentState)
        {
            case HordeState.WaitingForWaveStart:
                StartWave();
                break;
            case HordeState.SpawningEntry:
                AdvanceSpawnEntry();
                break;
            case HordeState.WaitingForEntryInterval:
                BeginNextEntry();
                break;
            case HordeState.WaitingForPostEntryCooldown:
                CompleteCurrentEntry();
                break;
            case HordeState.Completed:
            case HordeState.Uninitialized:
            default:
                break;
        }
    }

    /// <summary>
    /// Starts the current wave and begins the first spawn entry.
    /// </summary>
    private void StartWave()
    {
        if (!HasRemainingWaves())
        {
            CompleteHorde();
            return;
        }

        UpdateWinCondition();
        currentEntryIndex = 0;
        currentSpawnEntryIndex = 0;
        currentSpawnedInEntry = 0;
        currentState = HordeState.SpawningEntry;
        AdvanceSpawnEntry();
    }

    /// <summary>
    /// Advances the current spawn entry by spawning the next unit or moving to cooldown.
    /// </summary>
    private void AdvanceSpawnEntry()
    {
        HordeWaveSO wave = CurrentWave;
        if (wave == null || currentEntryIndex < 0 || currentEntryIndex >= wave.spawnEntries.Count)
        {
            CompleteWave();
            return;
        }

        WaveSpawnEntrySO entry = wave.spawnEntries[currentEntryIndex];

        if (currentSpawnedInEntry < entry.spawnedAmount)
        {
            SpawnUnit(entry);
            currentSpawnedInEntry++;
            currentSpawnEntryIndex = currentEntryIndex;

            if (currentSpawnedInEntry < entry.spawnedAmount)
            {
                currentState = HordeState.SpawningEntry;
                stateTimer = new LoopingTimer { Time = entry.spawnInterval, Interval = entry.spawnInterval };
            }
            else
            {
                currentState = HordeState.WaitingForPostEntryCooldown;
                stateTimer = new LoopingTimer { Time = entry.postSpawnCooldown, Interval = entry.postSpawnCooldown };
            }

            return;
        }

        CompleteCurrentEntry();
    }

    /// <summary>
    /// Finishes the current entry and transitions to either the next entry or the next wave.
    /// </summary>
    private void CompleteCurrentEntry()
    {
        HordeWaveSO wave = CurrentWave;
        if (wave == null)
        {
            CompleteWave();
            return;
        }

        if (currentEntryIndex + 1 < wave.spawnEntries.Count)
        {
            currentState = HordeState.WaitingForEntryInterval;
            stateTimer = new LoopingTimer { Time = wave.entryInterval, Interval = wave.entryInterval };
            currentSpawnEntryIndex = -1;
        }
        else
        {
            CompleteWave();
        }
    }

    /// <summary>
    /// Begins the next entry after the configured inter-entry delay.
    /// </summary>
    private void BeginNextEntry()
    {
        HordeWaveSO wave = CurrentWave;
        if (wave == null)
        {
            CompleteWave();
            return;
        }

        currentEntryIndex++;
        if (currentEntryIndex >= wave.spawnEntries.Count)
        {
            CompleteWave();
            return;
        }

        currentSpawnEntryIndex = currentEntryIndex;
        currentSpawnedInEntry = 0;
        currentState = HordeState.SpawningEntry;
        AdvanceSpawnEntry();
    }

    /// <summary>
    /// Completes the current wave and schedules the next one if available.
    /// </summary>
    private void CompleteWave()
    {
        currentSpawnEntryIndex = -1;
        currentSpawnedInEntry = 0;
        currentEntryIndex = -1;

        currentWaveIndex++;
        UpdateWinCondition();

        if (HasRemainingWaves())
        {
            float nextDelay = CurrentWave.nextWaveDelay;
            BeginWaveCountdown(nextDelay);
        }
        else
        {
            CompleteHorde();
        }
    }

    /// <summary>
    /// Marks the entire horde progression as complete.
    /// </summary>
    private void CompleteHorde()
    {
        currentState = HordeState.Completed;
        finalWave = true;
        currentSpawnEntryIndex = -1;
        currentSpawnedInEntry = 0;
        currentEntryIndex = -1;
        spawnEntryRemainingInterval = 0f;
        spawnEntryPostCooldownRemaining = 0f;
        stateTimer = new LoopingTimer { Time = 0f, Interval = 0f };

        Debug.Log("[Horde] All waves completed.");
        OnFinalWaveSpawn?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Returns true when there are additional waves to play.
    /// </summary>
    /// <returns>True if additional waves remain.</returns>
    private bool HasRemainingWaves()
    {
        return hordeWaveRegistrySO != null && currentWaveIndex < hordeWaveRegistrySO.hordeWaveSOs.Count;
    }

    /// <summary>
    /// Returns the current wave definition.
    /// </summary>
    private HordeWaveSO CurrentWave => HasRemainingWaves() ? hordeWaveRegistrySO.hordeWaveSOs[currentWaveIndex] : null;

    /// <summary>
    /// Returns the current state value for save serialization.
    /// </summary>
    public int CurrentState => (int)currentState;

    /// <summary>
    /// Returns the current state timer value for save serialization.
    /// </summary>
    public float CurrentStateTimer => stateTimer.Time;

    /// <summary>
    /// Returns the current interval value for save serialization.
    /// </summary>
    public float CurrentTimerInterval => stateTimer.Interval;

    /// <summary>
    /// Applies saved horde progression state.
    /// </summary>
    /// <param name="horde">Horde state loaded from save.</param>
    public void ApplyManagedData(DtoHordeData horde)
    {
        currentWaveIndex = horde.currentWaveIndex;
        currentState = (HordeState)Mathf.Clamp(horde.currentState, 0, (int)HordeState.Completed);
        stateTimer = new LoopingTimer
        {
            Time = Mathf.Max(0f, horde.currentStateTimer),
            Interval = Mathf.Max(0f, horde.currentTimerInterval)
        };
        finalWave = horde.finalWave;
        lastPoolIndex = horde.lastPoolIndex;

        currentEntryIndex = horde.currentEntryIndex;
        currentSpawnEntryIndex = horde.currentSpawnEntryIndex;
        currentSpawnedInEntry = horde.currentSpawnedInEntry;
        spawnEntryRemainingInterval = horde.spawnEntryRemainingInterval;
        spawnEntryPostCooldownRemaining = horde.spawnEntryPostCooldownRemaining;

        if (currentState == HordeState.WaitingForWaveStart && !HasRemainingWaves())
            CompleteHorde();

        UpdateWinCondition();
        UpdateDebugIntervals();
    }

    /// <summary>
    /// Spawns a single ECS unit at a selected spawn point.
    /// </summary>
    /// <param name="entry">Spawn entry defining unit type.</param>
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
    /// Returns a spawn point based on direction or randomized pool selection.
    /// Ensures reduced repetition of spawn locations.
    /// </summary>
    /// <param name="direction">Requested spawn direction.</param>
    /// <returns>Valid spawn transform.</returns>
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
    /// Returns spawn pool for a given directional input.
    /// </summary>
    /// <param name="direction">Spawn direction.</param>
    /// <returns>Directional spawn pool.</returns>
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
    /// Randomly selects a spawn pool while avoiding immediate repetition.
    /// </summary>
    /// <returns>Random spawn pool.</returns>
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
    /// Returns human-readable name of a spawn pool for debugging purposes.
    /// </summary>
    /// <param name="pool">Spawn pool.</param>
    /// <returns>Pool name string.</returns>
    private string GetPoolName(List<Transform> pool)
    {
        if (pool == northSpawnPoints) return "North";
        if (pool == westSpawnPoints) return "West";
        if (pool == southSpawnPoints) return "South";
        if (pool == eastSpawnPoints) return "East";
        return "Unknown";
    }
}

/// <summary>
/// Defines the current horde state for various save/load interactions.
/// </summary>
internal enum HordeState
{
    Uninitialized = 0,
    WaitingForWaveStart = 1,
    SpawningEntry = 2,
    WaitingForEntryInterval = 3,
    WaitingForPostEntryCooldown = 4,
    Completed = 5
}

/// <summary>
/// Defines possible spawn directions for horde waves.
/// </summary>
public enum WaveSpawnPoint
{
    Random,
    North,
    West,
    South,
    East
}