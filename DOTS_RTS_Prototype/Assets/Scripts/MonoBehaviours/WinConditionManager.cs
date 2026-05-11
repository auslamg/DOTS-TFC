using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class WinConditionManager : MonoBehaviour
{
    [Header("Win conditions")]

    /// <summary>
    /// .
    /// </summary>
    [SerializeField]
    [Tooltip(".")]
    private bool finalWave = false;

    /// <summary>
    /// .
    /// </summary>
    [SerializeField]
    [Tooltip(".")]
    private int remainingEnemies = 0;
    private EntityManager entityManager;

    public event EventHandler OnVictory;
    public static WinConditionManager Instance { get; private set; }

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

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        HordeManager.Instance.OnFinalWaveSpawn += HordeManager_OnFinalWaveSpawn;
    }

    private void HordeManager_OnFinalWaveSpawn(object sender, EventArgs e)
    {
        finalWave = true;
        CheckWinConditions();
    }

    void Update()
    {
        CheckWinConditions();
    }

    private void CheckWinConditions()
    {
        CheckForRemainingEnemies();

        // 5 is a safety measure to avoid stuck enemies preventing from wining.
        if (finalWave && remainingEnemies <= 5)
        {
            OnVictory?.Invoke(this, EventArgs.Empty);
        }
    }

    private void CheckForRemainingEnemies()
    {
        EntityQuery query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<Unit>(),
                ComponentType.ReadOnly<Faction>());

        NativeArray<Faction> factionArray =
            query.ToComponentDataArray<Faction>(Allocator.Temp);

        int enemyCount = 0;

        foreach (Faction faction in factionArray)
        {
            if (faction.factionID == GameAssets.ENEMY_FACTION)
            {
                enemyCount++;
            }
        }

        factionArray.Dispose();

        remainingEnemies = enemyCount;
    }
}
