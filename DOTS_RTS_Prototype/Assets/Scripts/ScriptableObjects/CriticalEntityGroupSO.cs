using UnityEngine;

[CreateAssetMenu(fileName = "CriticalEntityGroupSO", menuName = "LossConditions/CriticalEntityGroupSO")]
public class CriticalEntityGroupSO : ScriptableObject
{
    public string groupName;
    private void OnValidate()
    {
        groupName = this.name;
    }
}
