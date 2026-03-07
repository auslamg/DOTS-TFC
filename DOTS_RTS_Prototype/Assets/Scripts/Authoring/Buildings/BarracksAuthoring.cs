using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="Barracks"/> unmanaged component.
/// </summary>
//TODO: Implement owners with ID's as Data
public class BarracksAuthoring : MonoBehaviour
{
    public float maxProgress;
    public Transform spawnPoint;

    public Transform defaultRallyPoint;
}

/// <summary>
/// Baker for the <see cref="Barracks"/> unmanaged component.
/// </summary>
class BarracksBaker : Baker<BarracksAuthoring>
{
    public override void Bake(BarracksAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Barracks
        {
            maxProgress = authoring.maxProgress,
            spawnPointOffset = authoring.spawnPoint.position - authoring.transform.position,
            rallyPositionOffset = authoring.defaultRallyPoint.position - authoring.transform.position
        });

        DynamicBuffer<SpawnUnitsBuffer> spawnUnitsDynamicBuffer = AddBuffer<SpawnUnitsBuffer>(entity);
        AddExampleUnitsQueue(spawnUnitsDynamicBuffer);
    }

    private static void AddExampleUnitsQueue(DynamicBuffer<SpawnUnitsBuffer> spawnUnitsDynamicBuffer)
    {
        spawnUnitsDynamicBuffer.Add(new SpawnUnitsBuffer
        {
            unitKey = new UnitKey
            {
                name = "Soldier",
                unitType = UnitType.Ranged
            }
        });
        spawnUnitsDynamicBuffer.Add(new SpawnUnitsBuffer
        {
            unitKey = new UnitKey
            {
                name = "Soldier",
                unitType = UnitType.Ranged
            }
        });
        spawnUnitsDynamicBuffer.Add(new SpawnUnitsBuffer
        {
            unitKey = new UnitKey
            {
                name = "Scout",
                unitType = UnitType.Ranged
            }
        });
    }
}

/// <summary>
/// Used by the barracks building.
/// </summary>
public struct Barracks : IComponentData
{
    public float currentProgress;
    public float maxProgress;
    public UnitKey activeUnitKey;
    public float3 spawnPointOffset;
    public float3 rallyPositionOffset;
}

[InternalBufferCapacity(10)]
public struct SpawnUnitsBuffer : IBufferElementData
{
    public UnitKey unitKey;
}