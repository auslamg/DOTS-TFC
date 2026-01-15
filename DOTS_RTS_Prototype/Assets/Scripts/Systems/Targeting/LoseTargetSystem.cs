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
            if (!EntityUtil.ExistsAndPersists(ref state, targetter.ValueRO.targetEntity))
            {
                //No target, reset timer

                loseTarget.ValueRW.attemptPhaseTime = loseTarget.ValueRO.attemptFrequency;
            }
            else
            {
                if (EntityUtil.ExistsAndPersists(ref state, manualTarget.ValueRO.targetEntity))
                {
                    //There's a manual target, don't lose it

                    loseTarget.ValueRW.attemptPhaseTime = loseTarget.ValueRO.attemptFrequency;
                }
                else
                {
                    //IDEA: Refactor into corroutines
                    //Timer
                    loseTarget.ValueRW.attemptPhaseTime -= SystemAPI.Time.DeltaTime;
                    if (loseTarget.ValueRO.attemptPhaseTime <= 0)
                    {
                        loseTarget.ValueRW.attemptPhaseTime = loseTarget.ValueRO.attemptFrequency;

                        //Check if the target is far enough to attempt losing it
                        LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(targetter.ValueRO.targetEntity);
                        float targetDistance = math.distance(LocalTransform.ValueRO.Position, targetLocalTransform.Position);
                        if (targetDistance > loseTarget.ValueRO.thresholdDistance)
                        {
                            targetter.ValueRW.targetEntity = Entity.Null;
                        }
                    }
                }
            }
        }
    }
}