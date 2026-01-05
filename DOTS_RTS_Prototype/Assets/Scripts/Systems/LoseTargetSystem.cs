using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct LoseTargetSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRO<LocalTransform> LocalTransform,
            RefRW<Targetter> targetter,
            RefRO<LoseTarget> loseTarget,
            RefRO<TargetOverride> targetOverride)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<Targetter>,
                RefRO<LoseTarget>,
                RefRO<TargetOverride>>())
        {
            //FIX: Avoid continue. Maybe labels/goto?
            if (!state.EntityManager.ExistsAndPersists(targetter.ValueRO.targetEntity))
            {
                continue;
            }

            //FIX: Avoid continue. Maybe labels/goto?
            if (state.EntityManager.ExistsAndPersists(targetOverride.ValueRO.targetEntity))
            {
                continue;
            }

            LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(targetter.ValueRO.targetEntity);
            float targetDistance = math.distance(LocalTransform.ValueRO.Position, targetLocalTransform.Position);

            if (targetDistance > loseTarget.ValueRO.thresholdDistance)
            {
                //Target is too far > Reset it
                targetter.ValueRW.targetEntity = Entity.Null;
            }
        }
    }
}
