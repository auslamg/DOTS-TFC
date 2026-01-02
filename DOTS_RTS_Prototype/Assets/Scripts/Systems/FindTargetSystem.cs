using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

partial struct FindTargetSystem : ISystem
{
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
                RefRW<Targetter>>()
             )
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
            //Scan around entity
            if (collisionWorld.OverlapSphere(localTransform.ValueRO.Position, findTarget.ValueRO.targetRange, ref distanceHitList, collisionFilter))
            {
                foreach (DistanceHit distanceHit in distanceHitList)
                {
                    //TODO: Refactor into using owner IDs
                    //TODO: Add logic to find the closest target

                    //FIX: Avoid continue. Maybe labels/goto?
                    //IDEA: Extract into EntityUtil.Exists() method
                    if (!SystemAPI.Exists(distanceHit.Entity) || !SystemAPI.HasComponent<Unit>(distanceHit.Entity))
                    {
                        continue;
                    }

                    Unit targetUnit = SystemAPI.GetComponent<Unit>(distanceHit.Entity);
                    //FIX: Find alternative to break
                    if (targetUnit.faction == findTarget.ValueRO.targetFaction)
                    {
                        //Valid target
                        targetter.ValueRW.targetEntity = distanceHit.Entity;
                        break;
                    }
                }
            }
        }
    }
}
