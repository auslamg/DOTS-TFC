using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Authoring component that bakes data into <see cref="UnitDataRegistry"/>.
/// </summary>
/// <remarks>
/// Behaves as a scene singleton.
/// </remarks>
class UnitDataRegistryAuthoring : MonoBehaviour
{
    /// <summary>
    /// Source scriptable object containing all unit definitions.
    /// </summary>
    [SerializeField]
    [Tooltip("Scriptable object containing all unit definitions for the registry.")]
    public UnitDataRegistrySO unitRegistrySO;

    /// <summary>
    /// Scene singleton instance for managed-side access.
    /// </summary>
    public static UnitDataRegistryAuthoring Instance { get; private set; }

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
/// Baker for the <see cref="UnitDataRegistry"/> unmanaged component.
/// Builds blob data used for runtime unit lookups.
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
/// Singleton component containing all <see cref="UnitData"/> entries baked from <see cref="UnitDataRegistrySO"/>.
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
    /// <summary>
    /// Unique key for this unit data entry.
    /// </summary>
    public UnitKey unitKey;
    /// <summary>
    /// Category/type metadata for this unit.
    /// </summary>
    public UnitType unitType;
    /// <summary>
    /// Time required to train this unit in trainer systems.
    /// </summary>
    public float trainingTime;
}