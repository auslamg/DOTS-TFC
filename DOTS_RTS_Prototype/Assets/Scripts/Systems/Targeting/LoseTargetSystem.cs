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
            RefRW<LoseTarget> loseTarget,
            RefRO<ManualTarget> manualTarget)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<Targetter>,
                RefRW<LoseTarget>,
                RefRO<ManualTarget>>())
        {
            //FIX: Avoid continue. Maybe labels/goto?
            if (!EntityUtil.ExistsAndPersists(ref state, targetter.ValueRO.targetEntity))
            {
                loseTarget.ValueRW.attemptPhaseTime = loseTarget.ValueRO.attemptFrequency;
                continue;
            }

            //FIX: Avoid continue. Maybe labels/goto?
            if (EntityUtil.ExistsAndPersists(ref state, manualTarget.ValueRO.targetEntity))
            {
                loseTarget.ValueRW.attemptPhaseTime = loseTarget.ValueRO.attemptFrequency;
                continue;
            }

            //IDEA: Refactor into corroutines
            //FIX: Avoid continue. Maybe labels/goto?
            //Note: Only runs when other conditions are met. This is intentional
            loseTarget.ValueRW.attemptPhaseTime -= SystemAPI.Time.DeltaTime;
            if (loseTarget.ValueRO.attemptPhaseTime > 0)
            {
                continue;
            }
            loseTarget.ValueRW.attemptPhaseTime = loseTarget.ValueRO.attemptFrequency;

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
