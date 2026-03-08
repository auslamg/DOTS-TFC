using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingDataRegistrySO", menuName = "Buildings/BuildingDataRegistrySO")]
public class BuildingRegistrySO : ScriptableObject
{
    [SerializeField] public List<BuildingDataSO> buildingDataSOList;
    private Dictionary<BuildingKey, BuildingDataSO> buildingDataDictionary;
    

    private void OnEnable()
    {
        BuildDictionary();
    }

    private void BuildDictionary()
    {
        buildingDataDictionary = new Dictionary<BuildingKey, BuildingDataSO>();

        foreach (BuildingDataSO so in buildingDataSOList)
        {
            if (buildingDataDictionary.ContainsKey(so.buildingKey))
            {
                Debug.LogWarning($"Duplicate BuildingKey found: {so.buildingKey}", this);
                continue;
            }

            buildingDataDictionary.Add(so.buildingKey, so);
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
            buildingDataDictionary != null &&
            buildingDataDictionary.Count == buildingDataSOList.Count;
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

    public BuildingDataSO GetBuildingDataSO(BuildingKey buildingKey)
    {
        if (!IsVerified())
        {
            BuildDictionary();
        }

        if (buildingDataDictionary.TryGetValue(buildingKey, out var so))
        {
            return so;
        }

        Debug.LogError($"Could not find Building ScriptableObject for {buildingKey}", this);
        return null;
    }
}