using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HordeWaveRegistrySO", menuName = "Scriptable Objects/Horde Waves/HordeWaveRegistrySO")]
public class HordeWaveRegistrySO : ScriptableObject
{
    /// <summary>
    /// List of horde waves.
    /// </summary>
    [SerializeField]
    [Tooltip("List of horde waves.")]
    public List<HordeWaveSO> hordeWaveSOs;
}
