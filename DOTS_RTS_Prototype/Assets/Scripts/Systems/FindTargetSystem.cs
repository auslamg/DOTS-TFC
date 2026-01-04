using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

partial struct FindTargetSystem : ISystem
{
    //TODO: //BUG: Target changes even if there is already an alive target
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Register CollisionWorld for physics queries
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        //Used for registering all available targets
        NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.Temp); //Kept external to avoid excesive lists
        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<FindTarget> findTarget,
            RefRW<Targetter> targetter)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<FindTarget>,
                RefRW<Targetter>>())
        {
            //IDEA: Refactor into corroutines
            //FIX: Find alternative to continue
            findTarget.ValueRW.scanPhaseTime -= SystemAPI.Time.DeltaTime;
            if (findTarget.ValueRO.scanPhaseTime > 0)
            {
                continue;
            }
            findTarget.ValueRW.scanPhaseTime = findTarget.ValueRO.scanFrequency;

            distanceHitList.Clear();

            //CollisionFilter for physics query (OverlapSphere)
            CollisionFilter collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u, //All layers
                CollidesWith = 1u << GameAssets.UNITS_LAYER,
                GroupIndex = 0
            };

            //Closest target data
            Entity closestTargetEntity = Entity.Null;
            float closestTargetDistance = float.MaxValue;
            float swapTargetMinDistance = 0f;
            if (targetter.ValueRO.targetEntity != Entity.Null)
            {
                closestTargetEntity = targetter.ValueRO.targetEntity;
                LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(closestTargetEntity);
                closestTargetDistance = math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position);
                swapTargetMinDistance = findTarget.ValueRO.swapTargetMinDistance;
            }

            //Scan around entity
            if (collisionWorld.OverlapSphere(localTransform.ValueRO.Position, findTarget.ValueRO.targetRange, ref distanceHitList, collisionFilter))
            {
                foreach (DistanceHit distanceHit in distanceHitList)
                {
                    //TODO: Refactor into using owner IDs

                    //FIX: Avoid continue. Maybe labels/goto?
                    //IDEA: Extract into EntityUtil.Exists() method
                    if (!SystemAPI.Exists(distanceHit.Entity) || !SystemAPI.HasComponent<Unit>(distanceHit.Entity))
                    {
                        continue;
                    }

                    //Valid target with valid faction
                    Unit targetUnit = SystemAPI.GetComponent<Unit>(distanceHit.Entity);
                    if (targetUnit.faction == findTarget.ValueRO.targetFaction)
                    {
                        //Closest target logic
                        if (closestTargetEntity == Entity.Null)
                        {
                            closestTargetEntity = distanceHit.Entity;
                            closestTargetDistance = distanceHit.Distance;
                        }
                        else
                        {
                            if (distanceHit.Distance < closestTargetDistance)
                            {
                                closestTargetEntity = distanceHit.Entity;
                                closestTargetDistance = distanceHit.Distance;
                            }
                        }
                    }
                }
            }
            if (closestTargetEntity != Entity.Null)
            {
                targetter.ValueRW.targetEntity = closestTargetEntity;
            }

        }
    }
}
