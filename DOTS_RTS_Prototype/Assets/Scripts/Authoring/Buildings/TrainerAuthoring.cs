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
    public TrainRosterSO trainRosterSO;
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

        //Buffer for unit production queue
        DynamicBuffer<QueuedUnitBuffer> queuedUnitsDynamicBuffer = AddBuffer<QueuedUnitBuffer>(entity);
        AddExampleUnitsQueue(queuedUnitsDynamicBuffer, authoring.exampleUnitsQueue);


        //Verify constructed roster to ensure the Scriptable Object has deserialized data
        if (!authoring.trainRosterSO.VerifyConstruction())
        {
            Debug.Log($"Baking trainer roster went wrong in GameObject {authoring.gameObject.name}");
        }
        //Buffer for unit training roster
        DynamicBuffer<TrainableEntry> trainableEntriesBuffer = AddBuffer<TrainableEntry>(entity);
        foreach (UnitKey unitKey in authoring.trainRosterSO.unitKeySet)
        {
            trainableEntriesBuffer.Add(new TrainableEntry
            {
                unitKey = unitKey
            });
        }

        AddComponent(entity, new TrainUnitRequest());
        SetComponentEnabled<TrainUnitRequest>(entity, false);
    }

    private static void AddExampleUnitsQueue(DynamicBuffer<QueuedUnitBuffer> spawnUnitsDynamicBuffer, string[] exampleUnitsQueue)
    {
        foreach (string unitName in exampleUnitsQueue)
        {
            spawnUnitsDynamicBuffer.Add(new QueuedUnitBuffer
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
public struct QueuedUnitBuffer : IBufferElementData
{
    public UnitKey unitKey;
}

[InternalBufferCapacity(10)]
public struct TrainableEntry : IBufferElementData
{
    public UnitKey unitKey;
}

public struct TrainUnitRequest : IComponentData, IEnableableComponent
{
    public UnitKey unitKey;
}