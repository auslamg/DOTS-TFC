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
        // Used for registering all nearby units
        NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.Temp); //Kept external to avoid excesive lists
        
        CollisionWorld collisionWorld = state.EntityManager.GetCollisionWorld();

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
                                otherUnitMover.targetPosition += otherUnitMover.conflictResoultionOffset;
                                SystemAPI.SetComponent(distanceHit.Entity, otherUnitMover);
                            }

                            if (SystemAPI.IsComponentEnabled<FlowFieldFollower>(entity) &&
                                SystemAPI.IsComponentEnabled<FlowFieldFollower>(distanceHit.Entity))
                            {
                                FlowFieldFollower flowFieldFollower = SystemAPI.GetComponent<FlowFieldFollower>(entity);
                                FlowFieldFollower otherFlowFieldFollower = SystemAPI.GetComponent<FlowFieldFollower>(distanceHit.Entity);

                                if (otherFlowFieldFollower.postFormationPosition.Equals(flowFieldFollower.targetPosition))
                                {
                                    otherFlowFieldFollower.postFormationPosition += otherUnitMover.conflictResoultionOffset;
                                    SystemAPI.SetComponent(distanceHit.Entity, otherUnitMover);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
