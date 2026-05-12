using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HordeWaveSO", menuName = "Scriptable Objects/Horde Waves/HordeWaveSO")]
public class HordeWaveSO : ScriptableObject
{
    /// <summary>
    /// Series of horde entries to spawn in a wave.
    /// </summary>
    [SerializeField]
    [Tooltip("Series of horde entries to spawn in a wave.")]
    public List<WaveSpawnEntrySO> spawnEntries;

    /// <summary>
    /// Additional time to wait between horde entries.
    /// </summary>
    [Tooltip("Additional time to wait between horde entries.")]
    public float entryInterval;

    /// <summary>
    /// Time duration of the wave in seconds (time before next wave).
    /// </summary>
    [Tooltip("Time duration of the wave in seconds (time before next wave).")]
    public float waveDuration;
}

