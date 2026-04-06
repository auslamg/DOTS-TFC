using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(TargetFinderSystem))]
//TODO: Redocument
/// <summary>
/// Scans for valid enemy targets and assigns the best candidate to each targetter.
/// </summary>
/// <remarks>
/// Manual targets take priority. Automatic scans run on a timer and use overlap queries,
/// faction filtering, and closest-distance checks to pick target swaps.
/// </remarks>
partial struct TargetBuildingSeekerSystem : ISystem
{
    /// <summary>
    /// Performs timed target acquisition using physics overlap queries and faction checks.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Register CollisionWorld for physics queries
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        //Used for registering all available targets
        NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.Temp); //Kept external to avoid excesive lists
        foreach ((
            RefRW<TargetFinder> targetFinder,
            RefRW<Targetter> targetter,
            RefRO<Faction> faction,
            RefRO<TargetBuildingSeeker> buildingSeeker,
            RefRO<ManualTarget> manualTarget)
                in SystemAPI.Query<
                RefRW<TargetFinder>,
                RefRW<Targetter>,
                RefRO<Faction>,
                RefRO<TargetBuildingSeeker>,
                RefRO<ManualTarget>>())
        {
            if (targetFinder.ValueRO.scanPhaseTime == targetFinder.ValueRO.scanFrequency)
            {
                if (EntityUtil.ExistsAndPersists(ref state, targetter.ValueRO.targetEntity))
                {
                    return;
                }

                if (EntityUtil.ExistsAndPersists(ref state, manualTarget.ValueRO.targetEntity))
                {
                    return;
                }
 
                Debug.Log("Giving a try");

                //Get a building if there is no target, but set it as soft target so units prevail over buildings
                Entity targetBuildingEntity = Entity.Null;
                foreach ((
                    RefRO<Building> building,
                    RefRO<Faction> buildingFaction,
                    Entity buildingEntity)
                        in SystemAPI.Query<
                        RefRO<Building>,
                        RefRO<Faction>>().
                        WithEntityAccess())
                {
                    if (buildingFaction.ValueRO.factionID == faction.ValueRO.factionID)
                    {
                        continue;
                    }
                    else
                    {
                        targetBuildingEntity = buildingEntity;
                        break;
                    }
                }

                if (EntityUtil.ExistsAndPersists(ref state, targetBuildingEntity))
                {
                    Debug.Log($"Success: Target is + {targetBuildingEntity}");

                    targetter.ValueRW.targetEntity = targetBuildingEntity;
                }
            }
        }
    }
}
