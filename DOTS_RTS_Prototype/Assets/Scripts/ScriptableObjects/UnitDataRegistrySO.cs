using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitDataRegistrySO", menuName = "Units/UnitDataRegistrySO")]
public class UnitDataRegistrySO : ScriptableObject
{
    [SerializeField] public List<UnitDataSO> unitDataSOList;
    private Dictionary<UnitKey, UnitDataSO> unitDictionary;

    private void OnEnable()
    {
        BuildDictionary();
    }

    private void BuildDictionary()
    {
        unitDictionary = new Dictionary<UnitKey, UnitDataSO>();

        foreach (UnitDataSO so in unitDataSOList)
        {
            if (unitDictionary.ContainsKey(so.unitKey))
            {
                Debug.LogWarning($"Duplicate UnitKey found: {so.unitKey}", this);
                continue;
            }

            unitDictionary.Add(so.unitKey, so);
            /* Debug.Log($"Added unit: {so.unitKey}"); */
        }
    }

    /// <summary>
    /// Used to indicate if the internal Dictionary has already been verified to
    /// contain the elements of the serialized list.
    /// 
    /// This is because methods OnEnable() and OnValidate() build the dictionary BEFORE 
    /// the list is serialized, so it is verified in the first access to the Dictionary.
    /// </summary>
    private bool IsVerified()
    {
        return 
            unitDictionary != null &&
            unitDictionary.Count == unitDataSOList.Count;
    }

    public bool RebuildDictionary()
    {
        if (IsVerified())
        {
            return true;
        }
        else
        {
            BuildDictionary();
            return IsVerified();
        }
    }

    public UnitDataSO GetUnitSO(UnitKey unitKey)
    {
        if (!IsVerified())
        {
            BuildDictionary();
        }

        if (unitDictionary.TryGetValue(unitKey, out var so))
        {
            return so;
        }

        Debug.LogError($"Could not find Unit ScriptableObject for {unitKey}", this);
        return null;
    }
}