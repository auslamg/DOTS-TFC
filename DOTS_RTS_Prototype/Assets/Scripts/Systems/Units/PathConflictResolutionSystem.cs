using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(PathingRescheduleSystem))]
partial struct PathConflictResolutionSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        CollisionWorld collisionWorld = state.EntityManager.GetCollisionWorld();

        // Used for registering all nearby units
        NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.Temp); //Kept external to avoid excesive lists

        foreach ((
            RefRW<UnitMover> unitMover,
            RefRO<LocalTransform> localTransform,
            Entity entity)
            in SystemAPI.Query<
                RefRW<UnitMover>,
                RefRO<LocalTransform>>().
                WithPresent<FlowFieldFollower>().
                WithEntityAccess())
        {
            if (!unitMover.ValueRO.isMoving)
                continue;

            if (unitMover.ValueRW.conflictCheckTimer.Tick(Time.deltaTime))
            {
                distanceHitList.Clear();
                //CollisionFilter for physics query (OverlapSphere)
                CollisionFilter collisionFilter = new CollisionFilter
                {
                    BelongsTo = ~0u, //All layers
                    CollidesWith = 1u << GameAssets.UNITS_LAYER,
                    GroupIndex = 0
                };

                //Scan around entity
                if (collisionWorld.OverlapSphere(
                        position: localTransform.ValueRO.Position,
                        radius: unitMover.ValueRO.targetReachedDistanceSquared * 3f,
                        ref distanceHitList,
                        collisionFilter))
                {
                    foreach (DistanceHit distanceHit in distanceHitList)
                    {
                        //If an entity was hit
                        if (EntityUtil.ExistsAndPersists(ref state, distanceHit.Entity))
                        {
                            UnitMover otherUnitMover = SystemAPI.GetComponent<UnitMover>(distanceHit.Entity);
                            if (otherUnitMover.targetPosition.Equals(unitMover.ValueRO.targetPosition))
                            {
                                otherUnitMover.targetPosition += GenerateOffset(distanceHit.Entity);
                                SystemAPI.SetComponent(distanceHit.Entity, otherUnitMover);
                            }

                            if (SystemAPI.IsComponentEnabled<FlowFieldFollower>(entity) &&
                                SystemAPI.IsComponentEnabled<FlowFieldFollower>(distanceHit.Entity))
                            {
                                FlowFieldFollower flowFieldFollower = SystemAPI.GetComponent<FlowFieldFollower>(entity);
                                FlowFieldFollower otherFlowFieldFollower = SystemAPI.GetComponent<FlowFieldFollower>(distanceHit.Entity);

                                if (otherFlowFieldFollower.postFormationPosition.Equals(flowFieldFollower.targetPosition))
                                {
                                    otherFlowFieldFollower.postFormationPosition += GenerateOffset(distanceHit.Entity);
                                    SystemAPI.SetComponent(distanceHit.Entity, otherUnitMover);
                                }
                            }


                        }
                    }
                }
            }
        }
    }

    private float3 GenerateOffset(Entity entity)
    {
        // Create deterministic seed from entity identity
        uint seed = math.hash(new int2(entity.Index, entity.Version));

        // Random requires non-zero seed
        if (seed == 0)
            seed = 1;

        Unity.Mathematics.Random random = new Unity.Mathematics.Random(seed);

        // Random direction on XZ plane
        float angle = random.NextFloat(0f, math.PI * 2f);

        // Optional random distance
        float distance = random.NextFloat(1.5f, 3f);

        float2 direction = math.float2(
            math.cos(angle),
            math.sin(angle));

        return new float3(
            direction.x * distance,
            0f,
            direction.y * distance
        );
    }
}
