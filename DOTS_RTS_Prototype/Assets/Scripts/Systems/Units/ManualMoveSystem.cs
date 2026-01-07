using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct ManualMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<ManualMove> manualNove,
            EnabledRefRW<ManualMove> manualMoveEnabled,
            RefRW<UnitMover> unitMover)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<ManualMove>,
                EnabledRefRW<ManualMove>,
                RefRW<UnitMover>>())
        {
            if (math.distancesq(localTransform.ValueRO.Position, manualNove.ValueRO.targetPosition) > unitMover.ValueRO.targetReachedDistanceSquared)
            {
                //Move closer
                unitMover.ValueRW.targetPosition = manualNove.ValueRO.targetPosition;
            }
            else
            {
                //Reached move override position
                /* SystemAPI.SetComponentEnabled<MoveOverride>(false, entity); */
                manualMoveEnabled.ValueRW = false;
            }
        }
    }

}
