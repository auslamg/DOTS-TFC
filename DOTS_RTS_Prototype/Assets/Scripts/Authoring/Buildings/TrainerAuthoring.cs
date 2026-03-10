using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="Trainer"/> unmanaged component.
/// </summary>
//TODO: Implement owners with ID's as Data
public class TrainerAuthoring : MonoBehaviour
{
    public float maxProgress;
    public Transform spawnPoint;

    public Transform defaultRallyPoint;
    public string[] exampleUnitsQueue;
}

/// <summary>
/// Baker for the <see cref="Trainer"/> unmanaged component.
/// </summary>
class TrainerBaker : Baker<TrainerAuthoring>
{
    public override void Bake(TrainerAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Trainer
        {
            maxProgress = authoring.maxProgress,
            spawnPointOffset = authoring.spawnPoint.position - authoring.transform.position,
            rallyPositionOffset = authoring.defaultRallyPoint.position - authoring.transform.position
        });

        DynamicBuffer<SpawnUnitBuffer> spawnUnitsDynamicBuffer = AddBuffer<SpawnUnitBuffer>(entity);
        AddExampleUnitsQueue(spawnUnitsDynamicBuffer, authoring.exampleUnitsQueue);

        AddComponent(entity, new QueuedUnit
        {
            
        });
        SetComponentEnabled<QueuedUnit>(entity,false);
    }

    private static void AddExampleUnitsQueue(DynamicBuffer<SpawnUnitBuffer> spawnUnitsDynamicBuffer, string[] exampleUnitsQueue)
    {
        foreach (string unitName in exampleUnitsQueue)
        {
            spawnUnitsDynamicBuffer.Add(new SpawnUnitBuffer
            {
                unitKey = new UnitKey
                {
                    name = unitName
                }
            });
        }
    }
}

/// <summary>
/// Used by the trainer building.
/// </summary>
public struct Trainer : IComponentData
{
    public float currentProgress;
    public float maxProgress;
    public UnitKey activeUnitKey;
    public float3 spawnPointOffset;
    public float3 rallyPositionOffset;
    public bool onUnitQueueChange;
}

[InternalBufferCapacity(10)]
public struct SpawnUnitBuffer : IBufferElementData
{
    public UnitKey unitKey;
}

public struct QueuedUnit : IComponentData, IEnableableComponent
{
    public UnitKey unitKey;
}