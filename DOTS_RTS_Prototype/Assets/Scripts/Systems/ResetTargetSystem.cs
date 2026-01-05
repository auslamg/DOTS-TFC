using Unity.Burst;
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
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (RefRW<Targetter> targetter in SystemAPI.Query<RefRW<Targetter>>())
        {
            if (targetter.ValueRO.targetEntity != Entity.Null)
            {
                if (!state.EntityManager.ExistsAndPersists(targetter.ValueRO.targetEntity))
                {
                    targetter.ValueRW.targetEntity = Entity.Null;
                }
            }
        }

        foreach (RefRW<TargetOverride> targetOverride in SystemAPI.Query<RefRW<TargetOverride>>())
        {
            if (targetOverride.ValueRO.targetEntity != Entity.Null)
            {
                if (!state.EntityManager.ExistsAndPersists(targetOverride.ValueRO.targetEntity))
                {
                    targetOverride.ValueRW.targetEntity = Entity.Null;
                }
            }
        }
    }
}
