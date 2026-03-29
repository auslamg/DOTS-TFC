using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Authoring component that bakes data into <see cref="BuildingDataRegistry"/>.
/// </summary>
/// <remarks>
/// Behaves as a scene singleton.
/// </remarks>
class BuildingDataRegistryAuthoring : MonoBehaviour
{
    /// <summary>
    /// Source scriptable object containing all building definitions.
    /// </summary>
    [SerializeField]
    [Tooltip("Scriptable object containing all building definitions for the registry.")]
    public BuildingDataRegistrySO buildingRegistrySO;

    /// <summary>
    /// Scene singleton instance for managed-side access.
    /// </summary>
    public static BuildingDataRegistryAuthoring Instance { get; private set; }

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
/// Baker for the <see cref="BuildingDataRegistry"/> unmanaged component.
/// Builds blob data used for runtime building lookups.
/// </summary>
class BuildingDataRegistryBaker : Baker<BuildingDataRegistryAuthoring>
{
    public override void Bake(BuildingDataRegistryAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        //Verify constructed registry to ensure the Scriptable Object has deserialized data
        if (!authoring.buildingRegistrySO.VerifyConstruction())
        {
            Debug.Log($"Baking building data registry went wrong in GameObject {authoring.gameObject.name}");
        }

        //Sort items for binary search optimization
        BuildingDataSO[] sortedBuildings = authoring.buildingRegistrySO.buildingDataSOList
            .OrderBy((BuildingDataSO so) => so.buildingKey)
            .ToArray();


        BlobAssetReference<BlobArray<BuildingData>> blobAssetReference;
        //BlobBuilder resources
        using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
        {
            //Build new blob root
            ref BlobArray<BuildingData> root = ref blobBuilder.ConstructRoot<BlobArray<BuildingData>>();

            //Allocate memory for the building array in the root
            BlobBuilderArray<BuildingData> buildingIds =
                blobBuilder.Allocate<BuildingData>(ref root, sortedBuildings.Length);

            //For all Building ScriptableObjects found in the list reader
            for (int buildingIndex = 0; buildingIndex < buildingIds.Length; buildingIndex++)
            {
                BuildingDataSO buildingDataSO = sortedBuildings[buildingIndex];

                //Bake singular data inside blob entry
                BuildingData building = new BuildingData
                {
                    buildingKey = buildingDataSO.buildingKey,
                    buildingType = buildingDataSO.buildingType
                };

                buildingIds[buildingIndex] = building;
            }

            //Build BlobAssetReference
            blobAssetReference = blobBuilder.CreateBlobAssetReference<BlobArray<BuildingData>>(Allocator.Persistent);
        }

        AddComponent(entity, new BuildingDataRegistry
        {
            buildingBlobArrayReference = blobAssetReference
        });
    }
}


/// <summary>
/// Singleton component containing all <see cref="BuildingData"/> entries baked from <see cref="BuildingDataRegistrySO"/>.
/// </summary>
public struct BuildingDataRegistry : IComponentData
{
    /// <summary>
    /// Reference to the BlobArray containing all BuildingData.
    /// </summary>
    public BlobAssetReference<BlobArray<BuildingData>> buildingBlobArrayReference;
}

/// <summary>
/// Contains the building data baked from a <see cref="BuildingDataSO"/>.
/// </summary>
public struct BuildingData
{
    /// <summary>
    /// Unique key for this building data entry.
    /// </summary>
    public BuildingKey buildingKey;
    /// <summary>
    /// Category/type metadata for this building.
    /// </summary>
    public BuildingType buildingType;
}