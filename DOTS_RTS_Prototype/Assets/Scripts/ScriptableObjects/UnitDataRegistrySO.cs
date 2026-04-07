using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Registry ScriptableObject containing all <see cref="UnitDataSO"/> assets used by unit systems.
/// </summary>
/// <remarks>
/// Maintains a runtime dictionary keyed by <see cref="UnitKey"/> for fast managed lookups
/// and keeps the serialized list sorted for deterministic baking and binary-search workflows.
/// </remarks>
[CreateAssetMenu(fileName = "UnitDataRegistrySO", menuName = "Units/UnitDataRegistrySO")]
public class UnitDataRegistrySO : ScriptableObject
{
    /// <summary>
    /// Serialized unit data entries that populate this registry.
    /// </summary>
    [SerializeField]
    [Tooltip("Unit data entries included in this registry.")]
    public List<UnitDataSO> unitDataSOList;

    /// <summary>
    /// Runtime dictionary for fast key-based lookups.
    /// </summary>
    private Dictionary<UnitKey, UnitDataSO> unitDictionary;

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
        unitDictionary = new Dictionary<UnitKey, UnitDataSO>();

        foreach (UnitDataSO so in unitDataSOList)
        {
            if (unitDictionary.ContainsKey(so.unitKey))
            {
                if (so.unitKey.name != "")
                {
                    Debug.LogWarning($"Duplicate UnitKey found: {so.unitKey}", this);
                }
                continue;
            }

            unitDictionary.Add(so.unitKey, so);
            /* Debug.Log($"Added unit: {so.unitKey}"); */
        }

        unitDataSOList = unitDataSOList.OrderBy((UnitDataSO so) => so.name).ToHashSet().ToList();
    }

    /// <summary>
    /// Indicates whether cached dictionary state matches the serialized list.
    /// </summary>
    /// <returns><see langword="true"/> when cache and list counts match; otherwise <see langword="false"/>.</returns>
    private bool IsVerified()
    {
        return
            unitDictionary != null &&
            unitDictionary.Count == unitDataSOList.Count;
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
    /// Retrieves a unit data asset by key.
    /// </summary>
    /// <param name="unitKey">Unit key to retrieve.</param>
    /// <returns>Matching unit data asset, or <see langword="null"/> when not found.</returns>
    public UnitDataSO GetUnitSO(UnitKey unitKey)
    {
        if (!IsVerified())
        {
            Construct();
        }

        if (unitDictionary.TryGetValue(unitKey, out var so))
        {
            return so;
        }

        Debug.LogError($"Could not find unit data asset for key {unitKey}", this);
        return null;
    }
}