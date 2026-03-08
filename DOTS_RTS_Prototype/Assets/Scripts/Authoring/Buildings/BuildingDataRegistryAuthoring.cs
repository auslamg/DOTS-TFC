using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Managed component for the <see cref="BuildingDataRegistry"/> unmanaged component.
/// </summary>
/// <remarks>
/// The component is a Singleton.
/// </remarks>
class BuildingDataRegistryAuthoring : MonoBehaviour
{
    public BuildingDataRegistrySO buildingRegistrySO;

    public static BuildingDataRegistryAuthoring Instance { get; private set; }

    /// <summary>
    /// Used for singleton logic.
    /// </summary>
    void Awake()
    {
        //Singleton logic
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
/// Includes a blob building process for internal blob data.
/// </summary>
class BuildingDataRegistryBaker : Baker<BuildingDataRegistryAuthoring>
{
    public override void Bake(BuildingDataRegistryAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        if (authoring.buildingRegistrySO.RebuildDictionary())
        {
            Debug.Log($"Baking building entries: {authoring.buildingRegistrySO.buildingDataSOList.Count}");
        }

        //Sort items for binary search optimization
        BuildingDataSO[] sortedBuildings = authoring.buildingRegistrySO.buildingDataSOList
            .OrderBy((BuildingDataSO b) => b.buildingKey)
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
/// Contains all <see cref="BuildingData"/> baked from each <see cref="BuildingDataSO"/> in the <see cref="BuildingDataRegistrySO"/>.
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
    public BuildingKey buildingKey;
    public BuildingType buildingType;
}