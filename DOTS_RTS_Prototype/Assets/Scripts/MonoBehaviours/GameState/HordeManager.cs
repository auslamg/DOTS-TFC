using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class HordeManager : MonoBehaviour
{
    private EntityManager entityManager;

    [Header("Horde spawn points")]
    [SerializeField] private List<Transform> northSpawnPoints;
    [SerializeField] private List<Transform> westSpawnPoints;
    [SerializeField] private List<Transform> southSpawnPoints;
    [SerializeField] private List<Transform> eastSpawnPoints;

    [Header("Building spawn points")]
    [SerializeField] private List<Transform> lairSpawnPoints;

    [Header("Waves")]
    /// <summary>
    /// Registry of horde waves.
    /// </summary>
    [SerializeField]
    [Tooltip("Registry of horde waves.")]
    private HordeWaveRegistrySO hordeWaveRegistrySO;

    [SerializeField] private float initialWaveDelay = 10f;

    /// <summary>
    /// .
    /// </summary>
    [SerializeField]
    [Tooltip(".")]
    public int currentWaveIndex = 0;

    [SerializeField]
    [Tooltip(".")]
    public bool finalWave = false;

    [Header("Next Wave Timer")]
    [SerializeField] private LoopingTimer nextWaveTimer;

    public float remainingNextWaveTime => nextWaveTimer.Time;

    public bool isCountingDownToNextWave { get; private set; }

    public event EventHandler OnFinalWaveSpawn;

    public static HordeManager Instance { get; private set; }

    /// <summary>
    /// Initializes singleton instance state.
    /// </summary>
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple instances of singleton found on " + this.gameObject.name);
            Destroy(this);
        }
    }

    private void Awake()
    {
        InitializeSingleton();
    }

    private void TriggerFinalWaveSpawned()
    {
        Debug.Log("Final wave finished spawning.");

        OnFinalWaveSpawn?.Invoke(this, EventArgs.Empty);
    }

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        StartCoroutine(WaveLoop());
        UpdateWinCondition();
    }


    private void UpdateWinCondition()
    {
        if (currentWaveIndex >= hordeWaveRegistrySO.hordeWaveSOs.Count - 1)
        {
            finalWave = true;
        }
    }

    private IEnumerator WaveLoop()
    {
        // Initial countdown before first wave
        isCountingDownToNextWave = true;

        nextWaveTimer.Interval = initialWaveDelay;
        nextWaveTimer.Reset(false);

        while (!nextWaveTimer.Tick(Time.deltaTime))
        {
            yield return null;
        }

        isCountingDownToNextWave = false;

        while (currentWaveIndex < hordeWaveRegistrySO.hordeWaveSOs.Count)
        {
            HordeWaveSO wave =
                hordeWaveRegistrySO.hordeWaveSOs[currentWaveIndex];

            Debug.Log($"Starting Wave {currentWaveIndex}");

            yield return StartCoroutine(RunWave(wave));

            currentWaveIndex++;

            UpdateWinCondition();

            // Countdown until next wave
            if (!finalWave)
            {
                isCountingDownToNextWave = true;

                nextWaveTimer.Interval = wave.nextWaveDelay;
                nextWaveTimer.Reset(false);

                while (!nextWaveTimer.Tick(Time.deltaTime))
                {
                    yield return null;
                }

                isCountingDownToNextWave = false;
            }
        }

        Debug.Log("All waves completed.");

        finalWave = true;

        TriggerFinalWaveSpawned();
    }

    private IEnumerator RunWave(HordeWaveSO wave)
    {
        foreach (WaveSpawnEntrySO entry in wave.spawnEntries)
        {
            yield return StartCoroutine(RunEntry(entry));

            yield return new WaitForSeconds(wave.entryInterval);
        }
    }

    private IEnumerator RunEntry(WaveSpawnEntrySO entry)
    {
        for (int i = 0; i < entry.spawnedAmount; i++)
        {
            SpawnUnit(entry);

            yield return new WaitForSeconds(entry.spawnInterval);
        }

        yield return new WaitForSeconds(entry.postSpawnCooldown);
    }

    private void SpawnUnit(WaveSpawnEntrySO entry)
    {
        Transform spawnPoint = GetSpawnPoint(entry.spawnDirection);

        Debug.Log(
            $"Spawned {entry.spawnedUnitKey} at {spawnPoint.position}"
        );

        Entity spawnedEntity = entityManager.Instantiate(DataLookup.FetchEntityPrefab(EntityPrefabKey.From(entry.unitKey)));

        var localTransform = entityManager.GetComponentData<LocalTransform>(spawnedEntity);
        localTransform.Position = spawnPoint.position;
        entityManager.SetComponentData(spawnedEntity, localTransform);

    }

    private Transform GetSpawnPoint(WaveSpawnPoint direction)
    {
        List<Transform> pool = direction switch
        {
            WaveSpawnPoint.North => northSpawnPoints,
            WaveSpawnPoint.West => westSpawnPoints,
            WaveSpawnPoint.South => southSpawnPoints,
            WaveSpawnPoint.East => eastSpawnPoints,

            _ => GetRandomSpawnPool()
        };

        return pool[Random.Range(0, pool.Count)];
    }

    private List<Transform> GetRandomSpawnPool()
    {
        int roll = Random.Range(0, 4);

        return roll switch
        {
            0 => northSpawnPoints,
            1 => westSpawnPoints,
            2 => southSpawnPoints,
            _ => eastSpawnPoints
        };
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
