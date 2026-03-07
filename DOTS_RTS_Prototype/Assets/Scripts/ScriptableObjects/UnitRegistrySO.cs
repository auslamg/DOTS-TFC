using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitRegistrySO", menuName = "Units/UnitRegistrySO")]
public class UnitRegistrySO : ScriptableObject
{
    [SerializeField] public List<UnitSO> unitSOList;
    private Dictionary<UnitKey, UnitSO> unitDictionary;

    private void OnEnable()
    {
        BuildDictionary();
    }

    private void BuildDictionary()
    {
        unitDictionary = new Dictionary<UnitKey, UnitSO>();

        foreach (UnitSO so in unitSOList)
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
            unitDictionary.Count == unitSOList.Count;
    }

    public UnitSO GetUnitSO(UnitKey unitKey)
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