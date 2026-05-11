using UnityEngine;

[CreateAssetMenu(fileName = "HordeEntrySO", menuName = "Scriptable Objects/Horde Waves/WaveSpawnEntrySO")]
public class WaveSpawnEntrySO : ScriptableObject
{
    /// <summary>
    /// Identifier key for the spawned unit.
    /// </summary>
    [Tooltip("Identifier key for the spawned unit.")]
    public string spawnedUnitKey;

    /// <summary>
    /// Amount of spawned units of the specified type.
    /// </summary>
    [Tooltip("Amount of spawned units of the specified type.")]
    public int spawnedAmount;

    /// <summary>
    /// Map bounds direction from where to spawn the horde.
    /// </summary>
    [Tooltip("Map bounds direction from where to spawn the horde.")]
    public WaveSpawnPoint spawnDirection;

    /// <summary>
    /// Time interval between unit spawns within this entry.
    /// </summary>
    [Tooltip("Time interval between unit spawns within this entry.")]
    public float spawnInterval;

    /// <summary>
    /// Cooldown delay after the entry finishes to wait for the next entry.
    /// </summary>
    [Tooltip("Cooldown delay after the entry finishes to wait for the next entry.")]
    public float postSpawnCooldown;

    [SerializeField, HideInInspector]
    private UnitKey cachedUnitKey;

    /// <summary>
    /// Deterministic key generated from the asset name.
    /// </summary>
    public UnitKey unitKey => cachedUnitKey;

    /// <summary>
    /// Refreshes cached key data whenever the asset is modified in the editor.
    /// </summary>
    private void OnValidate()
    {
        cachedUnitKey = new UnitKey
        {
            name = spawnedUnitKey
        };
    }
}
