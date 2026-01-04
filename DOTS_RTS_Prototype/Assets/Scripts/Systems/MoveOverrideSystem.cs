using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct MoveOverrideSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<MoveOverride> moveOverride,
            EnabledRefRW<MoveOverride> moveOverrideEnabled,
            RefRW<UnitMover> unitMover)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<MoveOverride>,
                EnabledRefRW<MoveOverride>,
                RefRW<UnitMover>>()
                )
        {
            if (math.distancesq(localTransform.ValueRO.Position, moveOverride.ValueRO.targetPosition) > unitMover.ValueRO.targetReachedDistanceSquared)
            {
                //Move closer
                unitMover.ValueRW.targetPosition = moveOverride.ValueRO.targetPosition;
            }
            else
            {
                //Reached move override position
                /* SystemAPI.SetComponentEnabled<MoveOverride>(false, entity); */
                moveOverrideEnabled.ValueRW = false;
            }
        }
    }

}
