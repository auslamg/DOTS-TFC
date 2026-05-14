using UnityEngine;

/// <summary>
/// Defines a single spawn entry within a horde wave.
/// </summary>
/// <remarks>
/// Each entry controls what unit is spawned, how many are spawned,
/// and timing behavior for spawning and cooldown between entries.
/// </remarks>
[CreateAssetMenu(fileName = "HordeEntrySO", menuName = "Scriptable Objects/Horde Waves/WaveSpawnEntrySO")]
public class WaveSpawnEntrySO : ScriptableObject
{
    /// <summary>
    /// String identifier for the unit type to spawn.
    /// </summary>
    [Tooltip("Identifier key for the spawned unit.")]
    public string spawnedUnitKey;

    /// <summary>
    /// Number of units to spawn for this entry.
    /// </summary>
    [Tooltip("Amount of spawned units of the specified type.")]
    public int spawnedAmount;

    /// <summary>
    /// Direction or map edge from which units will spawn.
    /// </summary>
    [Tooltip("Map bounds direction from where to spawn the horde.")]
    public WaveSpawnPoint spawnDirection;

    /// <summary>
    /// Delay between individual unit spawns within this entry.
    /// </summary>
    [Tooltip("Time interval between unit spawns within this entry.")]
    public float spawnInterval;

    /// <summary>
    /// Delay after completing this entry before the next entry begins.
    /// </summary>
    [Tooltip("Cooldown delay after the entry finishes to wait for the next entry.")]
    public float postSpawnCooldown;

    /// <summary>
    /// Cached deterministic unit key derived from <see cref="spawnedUnitKey"/>.
    /// </summary>
    [SerializeField, HideInInspector]
    private UnitKey cachedUnitKey;

    /// <summary>
    /// Deterministic key used internally to reference the unit type.
    /// </summary>
    public UnitKey unitKey => cachedUnitKey;

    /// <summary>
    /// Updates the cached unit key whenever the asset is modified in the editor.
    /// </summary>
    private void OnValidate()
    {
        cachedUnitKey = new UnitKey
        {
            name = spawnedUnitKey
        };
    }
}