using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Authoring component that bakes data into <see cref="ResourceDataRegistry"/>.
/// </summary>
/// <remarks>
/// Behaves as a scene singleton.
/// </remarks>
class ResourceRegistryAuthoring : MonoBehaviour
{
    /// <summary>
    /// Source scriptable object containing all resource definitions.
    /// </summary>
    [SerializeField]
    [Tooltip("Scriptable object containing all resource definitions for the registry.")]
    public ResourceRegistrySO resourceRegistrySO;

    /// <summary>
    /// Scene singleton instance for managed-side access.
    /// </summary>
    public static ResourceRegistryAuthoring Instance { get; private set; }

    /// <summary>
    /// Initializes singleton instance state.
    /// </summary>
    void Awake()
    {
        // Initialize singleton instance state.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple instances of singleton found on " + this.gameObject.name);
            Destroy(this);
        }
    }
}

/// <summary>
/// Baker for the <see cref="ResourceDataRegistry"/> unmanaged component.
/// Builds blob data used for runtime resource lookups.
/// </summary>
class ResourceRegistryBaker : Baker<ResourceRegistryAuthoring>
{
    public override void Bake(ResourceRegistryAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        if (authoring.resourceRegistrySO.VerifyConstruction())
        {
            Debug.Log($"Baking resource entries: {authoring.resourceRegistrySO.resourceSOList.Count}");
        }

        //Sort items for binary search optimization
        ResourceSO[] sortedResources = authoring.resourceRegistrySO.resourceSOList
            .OrderBy((ResourceSO so) => so.resourceKey)
            .ToArray();

        BlobAssetReference<BlobArray<ResourceData>> blobAssetReference;
        //BlobBuilder resources
        using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
        {
            //Build new blob root
            ref BlobArray<ResourceData> root = ref blobBuilder.ConstructRoot<BlobArray<ResourceData>>();

            //Allocate memory for the resource array in the root
            BlobBuilderArray<ResourceData> resourceIds =
                blobBuilder.Allocate<ResourceData>(ref root, sortedResources.Length);

            //For all Resource ScriptableObjects found in the list reader
            for (int resourceIndex = 0; resourceIndex < resourceIds.Length; resourceIndex++)
            {
                ResourceSO resourceSO = sortedResources[resourceIndex];

                //Bake singular data inside blob entry
                ResourceData resource = new ResourceData
                {
                    resourceKey = resourceSO.resourceKey,
                    resourceType = resourceSO.resourceType
                };

                resourceIds[resourceIndex] = resource;
            }

            //Build BlobAssetReference
            blobAssetReference = blobBuilder.CreateBlobAssetReference<BlobArray<ResourceData>>(Allocator.Persistent);
        }

        AddComponent(entity, new ResourceDataRegistry
        {
            resourceBlobArrayReference = blobAssetReference
        });
    }
}


/// <summary>
/// Singleton component containing all <see cref="ResourceData"/> entries baked from <see cref="ResourceRegistrySO"/>.
/// </summary>
public struct ResourceDataRegistry : IComponentData
{
    /// <summary>
    /// Reference to the BlobArray containing all ResourceData.
    /// </summary>
    public BlobAssetReference<BlobArray<ResourceData>> resourceBlobArrayReference;
}

/// <summary>
/// Contains the resource data baked from a <see cref="ResourceSO"/>.
/// </summary>
public struct ResourceData
{
    /// <summary>
    /// Unique key for this resource data entry.
    /// </summary>
    public ResourceKey resourceKey;
    /// <summary>
    /// Category/type metadata for this resource.
    /// </summary>
    public ResourceType resourceType;
}
