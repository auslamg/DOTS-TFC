using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

//For all targetters queried, if the target has been removed or is pending for destruction, set the target to null
/// <summary>
/// Resets the <c>targetEntity</c> field for all non-persistent targets on Targetter components. 
/// </summary>
/// <remarks>
/// This is required to avoid null entity errors, hence why it runs at
/// the start of the LateSimulationSystemGroup (after all systems that use Targetter.targetEntity references have finished).
/// </remarks>
[UpdateInGroup(typeof(LateSimulationSystemGroup), OrderFirst = true)]
partial struct ResetTargetSystem : ISystem
{
    private ComponentLookup<LocalTransform> localTransformLookup;
    private EntityStorageInfoLookup esiLookup;

    /// <summary>
    /// Initializes lookup caches used to validate target entity persistence.
    /// </summary>
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
        esiLookup = state.GetEntityStorageInfoLookup();
    }

    /// <summary>
    /// Refreshes lookups and schedules target cleanup jobs for automatic and manual targets.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        localTransformLookup.Update(ref state);
        esiLookup.Update(ref state);

        new ResetTargetterJob
        {
            localTransformComponentLookup = localTransformLookup,
            entityStorageInfoLookup = esiLookup
        }.ScheduleParallel();

        new ResetManualTargetJob
        {
            localTransformComponentLookup = localTransformLookup,
            entityStorageInfoLookup = esiLookup
        }.ScheduleParallel();
    }
}

[BurstCompile]
/// <summary>
/// Clears <see cref="Targetter.targetEntity"/> when the referenced entity no longer persists.
/// </summary>
public partial struct ResetTargetterJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> localTransformComponentLookup;
    [ReadOnly] public EntityStorageInfoLookup entityStorageInfoLookup;

    /// <summary>
    /// Validates a targetter reference and resets it to <see cref="Entity.Null"/> when invalid.
    /// </summary>
    public void Execute(ref Targetter targetter)
    {
        if (targetter.targetEntity != Entity.Null)
        {
            if (!entityStorageInfoLookup.Exists(targetter.targetEntity) ||
                !localTransformComponentLookup.HasComponent(targetter.targetEntity))
            {
                targetter.targetEntity = Entity.Null;
            }
        }
    }
}

[BurstCompile]
/// <summary>
/// Clears <see cref="ManualTarget.targetEntity"/> when the referenced entity no longer persists.
/// </summary>
public partial struct ResetManualTargetJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> localTransformComponentLookup;
    [ReadOnly] public EntityStorageInfoLookup entityStorageInfoLookup;

    /// <summary>
    /// Validates a manual target reference and resets it to <see cref="Entity.Null"/> when invalid.
    /// </summary>
    public void Execute(ref ManualTarget manualTarget)
    {
        if (manualTarget.targetEntity != Entity.Null)
        {
            if (!entityStorageInfoLookup.Exists(manualTarget.targetEntity) ||
                !localTransformComponentLookup.HasComponent(manualTarget.targetEntity))
            {
                manualTarget.targetEntity = Entity.Null;
            }
        }
    }
}