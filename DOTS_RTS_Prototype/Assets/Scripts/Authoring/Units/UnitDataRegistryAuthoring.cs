using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Managed component for the <see cref="UnitDataRegistry"/> unmanaged component.
/// </summary>
/// <remarks>
/// The component is a Singleton.
/// </remarks>
class UnitDataRegistryAuthoring : MonoBehaviour
{
    public UnitDataRegistrySO unitRegistrySO;

    public static UnitDataRegistryAuthoring Instance { get; private set; }

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
/// Baker for the <see cref="UnitDataRegistry"/> unmanaged component.
/// Includes a blob building process for internal blob data.
/// </summary>
class UnitDataRegistryBaker : Baker<UnitDataRegistryAuthoring>
{
    public override void Bake(UnitDataRegistryAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        if (authoring.unitRegistrySO.VerifyConstruction())
        {
            Debug.Log($"Baking unit entries: {authoring.unitRegistrySO.unitDataSOList.Count}");
        }

        //Sort items for binary search optimization
        UnitDataSO[] sortedUnits = authoring.unitRegistrySO.unitDataSOList
            .OrderBy((UnitDataSO so) => so.unitKey)
            .ToArray();

        BlobAssetReference<BlobArray<UnitData>> blobAssetReference;
        //BlobBuilder resources
        using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
        {
            //Build new blob root
            ref BlobArray<UnitData> root = ref blobBuilder.ConstructRoot<BlobArray<UnitData>>();

            //Allocate memory for the unit array in the root
            BlobBuilderArray<UnitData> unitIds =
                blobBuilder.Allocate<UnitData>(ref root, sortedUnits.Length);

            //For all Unit ScriptableObjects found in the list reader
            for (int unitIndex = 0; unitIndex < unitIds.Length; unitIndex++)
            {
                UnitDataSO unitDataSO = sortedUnits[unitIndex];

                //Bake singular data inside blob entry
                UnitData unit = new UnitData
                {
                    unitKey = unitDataSO.unitKey,
                    unitType = unitDataSO.unitType,
                    trainingTime = unitDataSO.trainingTime
                };

                unitIds[unitIndex] = unit;
            }

            //Build BlobAssetReference
            blobAssetReference = blobBuilder.CreateBlobAssetReference<BlobArray<UnitData>>(Allocator.Persistent);
        }

        AddComponent(entity, new UnitDataRegistry
        {
            unitBlobArrayReference = blobAssetReference
        });
    }
}


/// <summary>
/// Contains all <see cref="UnitData"/> baked from each <see cref="UnitDataSO"/> in the <see cref="UnitDataRegistrySO"/>.
/// </summary>
public struct UnitDataRegistry : IComponentData
{
    /// <summary>
    /// Reference to the BlobArray containing all UnitData.
    /// </summary>
    public BlobAssetReference<BlobArray<UnitData>> unitBlobArrayReference;
}

/// <summary>
/// Contains the unit data baked from a <see cref="UnitDataSO"/>.
/// </summary>
public struct UnitData
{
    public UnitKey unitKey;
    public UnitType unitType;
    public float trainingTime;
}