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
    /// <summary>
    /// Progress value required to complete one unit training cycle.
    /// </summary>
    [SerializeField]
    [Tooltip("Progress required to complete one unit training cycle.")]
    public float maxProgress;
    /// <summary>
    /// Transform used as unit spawn point. Baked as local offset.
    /// </summary>
    [SerializeField]
    [Tooltip("Transform used as the spawn point for newly trained units.")]
    public Transform spawnPoint;

    /// <summary>
    /// Default rally point transform for newly spawned units. Baked as local offset.
    /// </summary>
    [SerializeField]
    [Tooltip("Default rally point for newly trained units.")]
    public Transform defaultRallyPoint;
    /// <summary>
    /// Optional startup queue of unit keys to prefill the production queue.
    /// </summary>
    [SerializeField]
    [Tooltip("Optional unit key queue used to prefill the trainer production queue.")]
    public string[] exampleUnitsQueue;
    /// <summary>
    /// Scriptable object roster defining units this trainer can produce.
    /// </summary>
    [SerializeField]
    [Tooltip("Roster scriptable object listing units this trainer can produce.")]
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
    /// <summary>
    /// Current accumulated training progress for <see cref="activeUnitKey"/>.
    /// </summary>
    public float currentProgress;
    /// <summary>
    /// Progress required to complete one training cycle.
    /// </summary>
    public float maxProgress;
    /// <summary>
    /// Unit key currently being trained.
    /// </summary>
    public UnitKey activeUnitKey;
    /// <summary>
    /// Local offset from trainer origin where trained units spawn.
    /// </summary>
    public float3 spawnPointOffset;
    /// <summary>
    /// Local offset from trainer origin used as rally destination.
    /// </summary>
    public float3 rallyPositionOffset;
    /// <summary>
    /// Event flag toggled when the training queue changes.
    /// </summary>
    public bool onUnitQueueChange;
}

/// <summary>
/// Queue element representing one requested unit to train.
/// </summary>
[InternalBufferCapacity(10)]
public struct QueuedUnitBuffer : IBufferElementData
{
    /// <summary>
    /// Unit key queued for training.
    /// </summary>
    public UnitKey unitKey;
}

/// <summary>
/// Roster entry listing one unit key that this trainer can produce.
/// </summary>
[InternalBufferCapacity(10)]
public struct TrainableEntry : IBufferElementData
{
    /// <summary>
    /// Unit key allowed by the trainer roster.
    /// </summary>
    public UnitKey unitKey;
}

/// <summary>
/// Enableable request component used to enqueue a new unit training command.
/// </summary>
public struct TrainUnitRequest : IComponentData, IEnableableComponent
{
    /// <summary>
    /// Unit key requested for training.
    /// </summary>
    public UnitKey unitKey;
}