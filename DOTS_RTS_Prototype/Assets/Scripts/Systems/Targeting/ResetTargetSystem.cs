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

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityUtil.GetEntityLookups(ref state, out localTransformLookup, out esiLookup);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityUtil.UpdateEntityLookups(ref state, ref localTransformLookup, ref esiLookup);

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
public partial struct ResetTargetterJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> localTransformComponentLookup;
    [ReadOnly] public EntityStorageInfoLookup entityStorageInfoLookup;
    public void Execute(ref Targetter targetter)
    {
        if (targetter.targetEntity != Entity.Null)
        {
            if (!targetter.targetEntity.ExistsAndPersists(entityStorageInfoLookup, localTransformComponentLookup))
            {
                targetter.targetEntity = Entity.Null;
            }
        }
    }
}

[BurstCompile]
public partial struct ResetManualTargetJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> localTransformComponentLookup;
    [ReadOnly] public EntityStorageInfoLookup entityStorageInfoLookup;
    public void Execute(ref ManualTarget manualTarget)
    {
        if (manualTarget.targetEntity != Entity.Null)
        {
            if (!manualTarget.targetEntity.ExistsAndPersists(entityStorageInfoLookup, localTransformComponentLookup))
            {
                manualTarget.targetEntity = Entity.Null;
            }
        }
    }
}
