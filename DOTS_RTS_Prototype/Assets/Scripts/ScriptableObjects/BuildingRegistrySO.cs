using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingRegistrySO", menuName = "Buildings/BuildingRegistrySO")]
public class BuildingRegistrySO : ScriptableObject
{
    [SerializeField] public List<BuildingSO> buildingSOList;
    private Dictionary<BuildingKey, BuildingSO> buildingDictionary;
    

    private void OnEnable()
    {
        BuildDictionary();
    }

    private void BuildDictionary()
    {
        buildingDictionary = new Dictionary<BuildingKey, BuildingSO>();

        foreach (BuildingSO so in buildingSOList)
        {
            if (buildingDictionary.ContainsKey(so.buildingKey))
            {
                Debug.LogWarning($"Duplicate BuildingKey found: {so.buildingKey}", this);
                continue;
            }

            buildingDictionary.Add(so.buildingKey, so);
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
            buildingDictionary != null &&
            buildingDictionary.Count == buildingSOList.Count;
    }

    public BuildingSO GetBuildingSO(BuildingKey buildingKey)
    {
        if (!IsVerified())
        {
            BuildDictionary();
        }

        if (buildingDictionary.TryGetValue(buildingKey, out var so))
        {
            return so;
        }

        Debug.LogError($"Could not find Building ScriptableObject for {buildingKey}", this);
        return null;
    }
}