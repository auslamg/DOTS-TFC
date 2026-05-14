using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scriptable Object registry containing all defined horde waves used by the horde system.
/// </summary>
/// <remarks>
/// Acts as a central collection point for <see cref="HordeWaveSO"/> assets, allowing wave sequencing
/// and runtime wave management without hard-coded references.
/// </remarks>
[CreateAssetMenu(fileName = "HordeWaveRegistrySO", menuName = "Scriptable Objects/Horde Waves/HordeWaveRegistrySO")]
public class HordeWaveRegistrySO : ScriptableObject
{
    /// <summary>
    /// List of horde wave definitions used by the game progression system.
    /// </summary>
    [SerializeField]
    [Tooltip("List of horde waves.")]
    public List<HordeWaveSO> hordeWaveSOs;
}