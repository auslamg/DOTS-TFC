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
            RefRW<TargetFinder> targetFinder,
            RefRW<Targetter> targetter,
            RefRO<Faction> faction,
            RefRO<ManualTarget> manualTarget)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<TargetFinder>,
                RefRW<Targetter>,
                RefRO<Faction>,
                RefRO<ManualTarget>>())
        {
            //IDEA: Refactor into corroutines
            //Timer
            targetFinder.ValueRW.scanPhaseTime -= SystemAPI.Time.DeltaTime;
            if (targetFinder.ValueRO.scanPhaseTime <= 0)
            {
                targetFinder.ValueRW.scanPhaseTime = targetFinder.ValueRO.scanFrequency;

                if (EntityUtil.ExistsAndPersists(ref state, manualTarget.ValueRO.targetEntity))
                {
                    //There's a manual target, don't try to find a new one
                    targetter.ValueRW.targetEntity = manualTarget.ValueRO.targetEntity;
                }
                else //Find a new target
                {
                    distanceHitList.Clear();
                    //CollisionFilter for physics query (OverlapSphere)
                    CollisionFilter collisionFilter = new CollisionFilter
                    {
                        BelongsTo = ~0u, //All layers
                        CollidesWith = 1u << GameAssets.UNITS_LAYER | 1u << GameAssets.BUILDINGS_LAYER,
                        GroupIndex = 0
                    };

                    //Closest target data
                    Entity closestTargetEntity = Entity.Null;
                    float closestTargetDistance = float.MaxValue;
                    float swapTargetMinDistance = 0f;

                    if (EntityUtil.ExistsAndPersists(ref state, targetter.ValueRO.targetEntity))
                    {
                        closestTargetEntity = targetter.ValueRO.targetEntity;
                        LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(closestTargetEntity);
                        closestTargetDistance = math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position);
                        swapTargetMinDistance = targetFinder.ValueRO.swapTargetMinDistance;
                    }

                    //Scan around entity
                    if (collisionWorld.OverlapSphere(localTransform.ValueRO.Position, targetFinder.ValueRO.targetRange, ref distanceHitList, collisionFilter))
                    {
                        foreach (DistanceHit distanceHit in distanceHitList)
                        {
                            //IDEA: Refactor into using owner IDs
                            //If an entity was hit
                            if (EntityUtil.ExistsAndPersists(ref state, distanceHit.Entity) || SystemAPI.HasComponent<Faction>(distanceHit.Entity))
                            {
                                //Valid target with valid faction
                                Faction targetFaction = SystemAPI.GetComponent<Faction>(distanceHit.Entity);
                                if (faction.ValueRO.factionID != targetFaction.factionID &&
                                    faction.ValueRO.factionID != 0 &&
                                    targetFaction.factionID != 0)
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
                    }
                    if (closestTargetEntity != Entity.Null)
                    {
                        targetter.ValueRW.targetEntity = closestTargetEntity;
                    }
                }
            }
        }
    }
}
