using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Registry ScriptableObject containing all <see cref="BuildingDataSO"/> assets used by building systems.
/// </summary>
/// <remarks>
/// Maintains a runtime dictionary keyed by <see cref="BuildingKey"/> for fast managed lookups
/// and keeps the serialized list sorted for deterministic baking and binary-search workflows.
/// </remarks>
[CreateAssetMenu(fileName = "BuildingDataRegistrySO", menuName = "Buildings/BuildingDataRegistrySO")]
public class BuildingDataRegistrySO : ScriptableObject
{
    /// <summary>
    /// Cached reference to the "None" building entry.
    /// </summary>
    [HideInInspector]
    public BuildingDataSO none;

    /// <summary>
    /// Serialized building data entries that populate this registry.
    /// </summary>
    [SerializeField]
    [Tooltip("Building data entries included in this registry.")]
    public List<BuildingDataSO> buildingDataSOList;

    /// <summary>
    /// Runtime dictionary for fast key-based lookups.
    /// </summary>
    private Dictionary<BuildingKey, BuildingDataSO> buildingDataDictionary;

    /// <summary>
    /// Rebuilds cached lookup structures when the asset is loaded.
    /// </summary>
    private void OnEnable()
    {
        Construct();
    }

    /// <summary>
    /// Rebuilds runtime lookup structures from serialized list data.
    /// </summary>
    private void Construct()
    {
        buildingDataDictionary = new Dictionary<BuildingKey, BuildingDataSO>();

        foreach (BuildingDataSO so in buildingDataSOList)
        {
            if (buildingDataDictionary.ContainsKey(so.buildingKey))
            {
                if (so.buildingKey.name != "")
                {
                    Debug.LogWarning($"Duplicate BuildingKey found: {so.buildingKey}", this);
                }
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
    /// Indicates whether cached dictionary state matches the serialized list.
    /// </summary>
    /// <returns><see langword="true"/> when cache and list counts match; otherwise <see langword="false"/>.</returns>
    private bool IsVerified()
    {
        return
            buildingDataDictionary != null &&
            buildingDataDictionary.Count == buildingDataSOList.Count;
    }

    /// <summary>
    /// Ensures lookup cache is fully constructed and synchronized with serialized data.
    /// </summary>
    /// <returns><see langword="true"/> when cache verification succeeds; otherwise <see langword="false"/>.</returns>
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

    /// <summary>
    /// Retrieves a building data asset by key.
    /// </summary>
    /// <param name="buildingKey">Building key to retrieve.</param>
    /// <returns>Matching building data asset, or <see langword="null"/> when not found.</returns>
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

        Debug.LogError($"Could not find building data asset for key {buildingKey}", this);
        return null;
    }
}