using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct RandomWalkSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRW<RandomWalk> randomWalk,
            RefRW<UnitMover> unitMover,
            RefRO<LocalTransform> localTransform)
                in SystemAPI.Query<
                RefRW<RandomWalk>,
                RefRW<UnitMover>,
                RefRO<LocalTransform>>().
                WithDisabled<MoveOverride>())
        {
            float targetReachedDistanceSquared = unitMover.ValueRO.targetReachedDistanceSquared;
            if (math.distancesq(localTransform.ValueRO.Position, randomWalk.ValueRO.targetPostion) < targetReachedDistanceSquared)
            {
                //Target reached

                //Assign new random target
                Random random = randomWalk.ValueRO.random;
                float3 randomDirection = new float3(random.NextFloat3(-1, 1)) * new float3(1, 0, 1); //Removed Y axis
                randomDirection = math.normalize(randomDirection);

                //Set target to a random distance in calculated random direction
                randomWalk.ValueRW.targetPostion =
                    randomWalk.ValueRO.originPointPosition +
                    randomDirection * random.NextFloat(randomWalk.ValueRO.minDistance, randomWalk.ValueRO.maxDistance);

                //Overwrite random for sequence persistence
                randomWalk.ValueRW.random = random;
            }
            else
            {
                //Too far, move closer
                if (!unitMover.ValueRO.targetPosition.Equals(randomWalk.ValueRO.targetPostion))
                {
                    //Set target if not set
                    unitMover.ValueRW.targetPosition = randomWalk.ValueRO.targetPostion;
                }
            }
        }
    }

}
