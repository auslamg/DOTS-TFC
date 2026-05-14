using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ScriptableObject registry that stores and provides fast lookup access to <see cref="ResourceSO"/> assets
/// using a <see cref="ResourceKey"/> identifier.
/// </summary>
[CreateAssetMenu(fileName = "ResourceRegistrySO", menuName = "Scriptable Objects/Resources/ResourceRegistrySO")]
public class ResourceRegistrySO : ScriptableObject
{
    /// <summary>
    /// Serialized collection of all resource definitions included in this registry.
    /// Used as the source of truth for rebuilding runtime lookup data.
    /// </summary>
    [SerializeField]
    [Tooltip("Resource data entries included in this registry.")]
    public List<ResourceSO> resourceSOList;

    /// <summary>
    /// Runtime lookup table for fast access to resources by key.
    /// Rebuilt on asset load or when verification fails.
    /// </summary>
    private Dictionary<ResourceKey, ResourceSO> resourceDictionary;

    /// <summary>
    /// Unity callback invoked when the ScriptableObject is loaded or reloaded.
    /// Ensures runtime lookup structures are initialized.
    /// </summary>
    private void OnEnable()
    {
        Construct();
    }

    /// <summary>
    /// Builds or rebuilds the runtime dictionary from the serialized resource list.
    /// Also applies a deterministic ordering to the serialized list.
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
    /// Checks whether the runtime dictionary is initialized and matches the serialized list size.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the dictionary exists and is in sync with the serialized list;
    /// otherwise <see langword="false"/>.
    /// </returns>
    private bool IsVerified()
    {
        return
            resourceDictionary != null &&
            resourceDictionary.Count == resourceSOList.Count;
    }

    /// <summary>
    /// Ensures that the runtime lookup structures are constructed and synchronized with serialized data.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the registry is successfully verified after construction;
    /// otherwise <see langword="false"/>.
    /// </returns>
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
    /// Retrieves a <see cref="ResourceSO"/> associated with the specified <see cref="ResourceKey"/>.
    /// </summary>
    /// <param name="resourceKey">The key identifying the requested resource.</param>
    /// <returns>
    /// The matching <see cref="ResourceSO"/> if found; otherwise <see langword="null"/>.
    /// </returns>
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