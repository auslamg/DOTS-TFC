using System;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Manages win conditions for the game based on enemy population and wave state.
/// Tracks remaining enemies using ECS queries and triggers victory when conditions are met.
/// </summary>
public class WinConditionManager : MonoBehaviour
{
    [Header("Win conditions")]

    /// <summary>
    /// Indicates whether the game is currently in the final wave phase.
    /// When true, win conditions begin evaluating against a dynamic threshold.
    /// </summary>
    [SerializeField]
    private bool finalWave = false;

    /// <summary>
    /// Current number of remaining enemy units detected in the ECS world.
    /// Updated periodically via entity queries.
    /// </summary>
    [SerializeField]
    private int remainingEnemies = 0;

    /// <summary>
    /// Dynamic threshold representing the maximum allowed enemies before victory is triggered.
    /// Increases over time during the final wave to gradually relax win conditions.
    /// </summary>
    [SerializeField]
    private int maxEnemiesToWin = 0;

    /// <summary>
    /// ECS EntityManager reference used for querying active entities in the world.
    /// </summary>
    private EntityManager entityManager;

    /// <summary>
    /// Indicates whether victory has already been achieved.
    /// Prevents repeated win triggers.
    /// </summary>
    private bool hasWon = false;

    /// <summary>
    /// Event triggered when victory conditions are satisfied.
    /// </summary>
    public event EventHandler OnVictory;

    /// <summary>
    /// Event triggered when the remaining enemy count or win threshold changes.
    /// </summary>
    public event EventHandler<RemainingEnemiesEventArgs> OnRemainingEnemiesChange;

    /// <summary>
    /// Singleton instance of the WinConditionManager.
    /// </summary>
    public static WinConditionManager Instance { get; private set; }

    /// <summary>
    /// Unity lifecycle method. Initializes singleton instance.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// Unity lifecycle method. Initializes ECS access, subscribes to events, and starts win condition loops.
    /// </summary>
    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        HordeManager.Instance.OnFinalWaveSpawn += HordeManager_OnFinalWaveSpawn;

        StartCoroutine(WinConditionLoop());
        StartCoroutine(WinThresholdRampLoop());
    }

    /// <summary>
    /// Ensures only one instance of this manager exists.
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

    /// <summary>
    /// Callback invoked when the final wave begins.
    /// </summary>
    /// <param name="sender">Event source.</param>
    /// <param name="e">Event arguments.</param>
    private void HordeManager_OnFinalWaveSpawn(object sender, EventArgs e)
    {
        finalWave = true;
    }

    /// <summary>
    /// Continuously evaluates win conditions at fixed intervals until victory is achieved.
    /// </summary>
    private IEnumerator WinConditionLoop()
    {
        while (!hasWon)
        {
            CheckForRemainingEnemies();

            if (CheckWinConditions())
            {
                hasWon = true;
                OnVictory?.Invoke(this, EventArgs.Empty);
                yield break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    /// <summary>
    /// Gradually increases the allowable enemy threshold during the final wave phase.
    /// </summary>
    private IEnumerator WinThresholdRampLoop()
    {
        while (!hasWon)
        {
            if (finalWave)
            {
                maxEnemiesToWin++;
                OnRemainingEnemiesChange?.Invoke(
                    this,
                    new RemainingEnemiesEventArgs(remainingEnemies, maxEnemiesToWin));
            }

            yield return new WaitForSeconds(15f);
        }
    }

    /// <summary>
    /// Evaluates whether current game state satisfies win conditions.
    /// </summary>
    /// <returns>True if victory conditions are met.</returns>
    private bool CheckWinConditions()
    {
        return finalWave && remainingEnemies <= maxEnemiesToWin;
    }

    /// <summary>
    /// Queries the ECS world to count remaining enemy entities and updates internal state.
    /// Raises an event if the enemy count changes.
    /// </summary>
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

        if (enemyCount != remainingEnemies)
        {
            OnRemainingEnemiesChange?.Invoke(
                this,
                new RemainingEnemiesEventArgs(enemyCount, maxEnemiesToWin));
        }

        remainingEnemies = enemyCount;
    }
}

/// <summary>
/// Event arguments containing updated enemy count and win threshold information.
/// </summary>
public class RemainingEnemiesEventArgs : EventArgs
{
    /// <summary>
    /// Current number of remaining enemy units.
    /// </summary>
    public int remainingEnemies { get; }

    /// <summary>
    /// Current maximum allowed enemy threshold for victory conditions.
    /// </summary>
    public int maxEnemiesToWin { get; }

    /// <summary>
    /// Initializes a new instance of the event arguments.
    /// </summary>
    /// <param name="remainingEnemies">Current enemy count.</param>
    /// <param name="maxEnemiesToWin">Current win threshold.</param>
    public RemainingEnemiesEventArgs(int remainingEnemies, int maxEnemiesToWin)
    {
        this.remainingEnemies = remainingEnemies;
        this.maxEnemiesToWin = maxEnemiesToWin;
    }
}