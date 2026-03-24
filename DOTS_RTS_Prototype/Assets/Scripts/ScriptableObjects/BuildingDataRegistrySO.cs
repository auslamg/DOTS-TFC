using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingDataRegistrySO", menuName = "Buildings/BuildingDataRegistrySO")]
public class BuildingDataRegistrySO : ScriptableObject
{
    [HideInInspector] public BuildingDataSO none;
    [SerializeField] public List<BuildingDataSO> buildingDataSOList;
    private Dictionary<BuildingKey, BuildingDataSO> buildingDataDictionary;

    private void OnEnable()
    {
        Construct();
    }

    private void Construct()
    {
        buildingDataDictionary = new Dictionary<BuildingKey, BuildingDataSO>();

        foreach (BuildingDataSO so in buildingDataSOList)
        {
            if (buildingDataDictionary.ContainsKey(so.buildingKey))
            {
                Debug.LogWarning($"Duplicate BuildingKey found: {so.buildingKey}", this);
                continue;
            }
            if (so.buildingType == BuildingType.None && none == null)
            {
                none = so;
            }

            buildingDataDictionary.Add(so.buildingKey, so);
            /* Debug.Log($"Added unit: {so.buildingKey}"); */
        }
        buildingDataSOList = buildingDataSOList.OrderBy((BuildingDataSO so) => so.name).ToHashSet().ToList();
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

    public bool VerifyConstruction()
    {
        if (IsVerified())
        {
            return true;
        }
        else
        {
            Construct();
            return IsVerified();
        }
    }

    public BuildingDataSO GetBuildingDataSO(BuildingKey buildingKey)
    {
        if (!IsVerified())
        {
            Construct();
        }

        if (buildingDataDictionary.TryGetValue(buildingKey, out var so))
        {
            return so;
        }

        Debug.LogError($"Could not find Building ScriptableObject for {buildingKey}", this);
        return null;
    }
}