using UnityEngine;

/// <summary>
/// ScriptableObject representing one logical critical-entity group used by loss-condition tracking.
/// </summary>
[CreateAssetMenu(fileName = "CriticalEntityGroupSO", menuName = "LossConditions/CriticalEntityGroupSO")]
public class CriticalEntityGroupSO : ScriptableObject
{
    /// <summary>
    /// Group name used as tracking tag at runtime.
    /// </summary>
    [SerializeField]
    [Tooltip("Logical group name used by loss-condition tracking systems.")]
    public string groupName;

    /// <summary>
    /// Keeps <see cref="groupName"/> synchronized with the asset name.
    /// </summary>
    private void OnValidate()
    {
        groupName = this.name;
    }
}
