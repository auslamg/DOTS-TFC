using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

//TODO: Document extensively
//For all targetters queried, if the target has been removed or is pending for destruction, set the target to null
[UpdateInGroup(typeof(LateSimulationSystemGroup), OrderFirst = true)]
partial struct ResetTargetSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (
            RefRW<Targetter> targetter in SystemAPI.Query<RefRW<Targetter>>()
             )
        {
            if (targetter.ValueRO.targetEntity != Entity.Null)
            {
                //IDEA: Extract into EntityUtil.Exists() method
                if (!SystemAPI.Exists(targetter.ValueRO.targetEntity) || !SystemAPI.HasComponent<LocalTransform>(targetter.ValueRO.targetEntity))
                {
                    targetter.ValueRW.targetEntity = Entity.Null;
                }
            }
        }
    }
}
