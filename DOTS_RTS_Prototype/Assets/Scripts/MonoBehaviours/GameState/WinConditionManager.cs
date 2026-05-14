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
    /// Indicates whether the game has entered the final wave phase.
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
    /// Reference to the ECS EntityManager used to query active entities in the world.
    /// </summary>
    private EntityManager entityManager;

    /// <summary>
    /// Tracks whether the victory condition has already been achieved.
    /// Prevents multiple victory triggers.
    /// </summary>
    private bool hasWon = false;

    /// <summary>
    /// Event triggered when victory conditions are met.
    /// </summary>
    public event EventHandler OnVictory;

    /// <summary>
    /// Event triggered whenever the number of remaining enemies changes.
    /// Provides updated enemy count information.
    /// </summary>
    public event EventHandler<RemainingEnemiesEventArgs> OnRemainingEnemiesChange;

    /// <summary>
    /// Singleton instance of the WinConditionManager.
    /// Provides global access to the active manager instance.
    /// </summary>
    public static WinConditionManager Instance { get; private set; }

    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        HordeManager.Instance.OnFinalWaveSpawn += HordeManager_OnFinalWaveSpawn;

        StartCoroutine(WinConditionLoop());
        StartCoroutine(WinThresholdRampLoop());
    }

    /// <summary>
    /// Ensures only one instance of this manager exists in the scene.
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
    /// Marks the game as being in the final wave when triggered by the HordeManager.
    /// </summary>
    private void HordeManager_OnFinalWaveSpawn(object sender, EventArgs e)
    {
        finalWave = true;
    }

    /// <summary>
    /// Continuously evaluates win conditions once per second until victory is achieved.
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
    /// Gradually increases the allowed enemy threshold during the final wave phase.
    /// </summary>
    private IEnumerator WinThresholdRampLoop()
    {
        while (!hasWon)
        {
            if (finalWave)
            {
                maxEnemiesToWin++;
                OnRemainingEnemiesChange?.Invoke(this, new RemainingEnemiesEventArgs(remainingEnemies, maxEnemiesToWin));
            }

            yield return new WaitForSeconds(15f);
        }
    }

    /// <summary>
    /// Evaluates whether win conditions are currently satisfied.
    /// </summary>
    private bool CheckWinConditions()
    {
        // Dynamic threshold instead of fixed value
        return finalWave && remainingEnemies <= maxEnemiesToWin;
    }

    /// <summary>
    /// Counts remaining enemy entities using an ECS query and updates internal state.
    /// Triggers an event if the enemy count changes.
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
            OnRemainingEnemiesChange?.Invoke(this, new RemainingEnemiesEventArgs(enemyCount, maxEnemiesToWin));
        }

        remainingEnemies = enemyCount;
    }
}

/// <summary>
/// Event argument container for passing updated enemy count values.
/// </summary>
public class RemainingEnemiesEventArgs : EventArgs
{
    /// <summary>
    /// The updated number of remaining enemy units.
    /// </summary>
    public int remainingEnemies { get; }

    /// <summary>
    /// The updated number of remaining enemy units to account for a victory.
    /// </summary>
    public int maxEnemiesToWin { get; }

    public RemainingEnemiesEventArgs(int remainingEnemies, int maxEnemiesToWin)
    {
        this.remainingEnemies = remainingEnemies;
        this.maxEnemiesToWin = maxEnemiesToWin;
    }
}