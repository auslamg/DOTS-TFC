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

    public BuildingSO GetBuildingSO(BuildingKey buildingKey)
    {
        if (buildingDictionary.TryGetValue(buildingKey, out var so))
        {
            return so;
        }

        Debug.LogError($"Could not find Building ScriptableObject for {buildingKey}", this);
        return null;
    }
}