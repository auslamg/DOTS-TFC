using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ResourceRegistrySO", menuName = "Scriptable Objects/Resources/ResourceRegistrySO")]
public class ResourceRegistrySO : ScriptableObject
{
    /// <summary>
    /// Serialized resource data entries that populate this registry.
    /// </summary>
    [SerializeField]
    [Tooltip("Resource data entries included in this registry.")]
    public List<ResourceSO> resourceSOList;

    /// <summary>
    /// Runtime dictionary for fast key-based lookups.
    /// </summary>
    private Dictionary<ResourceKey, ResourceSO> resourceDictionary;

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
        resourceDictionary = new Dictionary<ResourceKey, ResourceSO>();

        foreach (ResourceSO so in resourceSOList)
        {
            if (resourceDictionary.ContainsKey(so.resourceKey))
            {
                if (so.resourceKey.name != "")
                {
                    Debug.LogWarning($"Duplicate ResourceKey found: {so.resourceKey}", this);
                }
                continue;
            }

            resourceDictionary.Add(so.resourceKey, so);
            /* Debug.Log($"Added resource: {so.resourceKey}"); */
        }

        resourceSOList = resourceSOList.OrderBy((ResourceSO so) => so.name).ToHashSet().ToList();
    }

    /// <summary>
    /// Indicates whether cached dictionary state matches the serialized list.
    /// </summary>
    /// <returns><see langword="true"/> when cache and list counts match; otherwise <see langword="false"/>.</returns>
    private bool IsVerified()
    {
        return
            resourceDictionary != null &&
            resourceDictionary.Count == resourceSOList.Count;
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
    /// Retrieves a resource data asset by key.
    /// </summary>
    /// <param name="resourceKey">Resource key to retrieve.</param>
    /// <returns>Matching resource data asset, or <see langword="null"/> when not found.</returns>
    public ResourceSO GetResourceSO(ResourceKey resourceKey)
    {
        if (!IsVerified())
        {
            Construct();
        }

        if (resourceDictionary.TryGetValue(resourceKey, out var so))
        {
            return so;
        }

        Debug.LogError($"Could not find resource data asset for key {resourceKey}", this);
        return null;
    }
}
