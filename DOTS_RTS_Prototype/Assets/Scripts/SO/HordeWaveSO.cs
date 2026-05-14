using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a single horde wave composed of multiple sequential spawn entries.
/// </summary>
/// <remarks>
/// Controls timing between spawn entries and delay before the next wave starts.
/// </remarks>
[CreateAssetMenu(fileName = "HordeWaveSO", menuName = "Scriptable Objects/Horde Waves/HordeWaveSO")]
public class HordeWaveSO : ScriptableObject
{
    /// <summary>
    /// Ordered list of spawn entries that compose this wave.
    /// </summary>
    [SerializeField]
    [Tooltip("Series of horde entries to spawn in a wave.")]
    public List<WaveSpawnEntrySO> spawnEntries;

    /// <summary>
    /// Additional delay between processing each spawn entry in the wave.
    /// </summary>
    [Tooltip("Additional time to wait between horde entries.")]
    public float entryInterval;

    /// <summary>
    /// Delay in seconds before the next wave begins after this wave completes.
    /// </summary>
    [Tooltip("Time before the next wave starts in seconds.")]
    public float nextWaveDelay;
}