using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Periodically spawns configured prefabs while enforcing nearby ally population caps.
/// </summary>
partial struct SpawnerSystem : ISystem
{
    /// <summary>
    /// Requires prefab registry data before runtime spawning starts.
    /// </summary>
    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntityPrefabsRegistry>();
    }


    /// <summary>
    /// Ticks spawn timers, scans local occupancy, and instantiates new units when allowed.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Used for prefab instancing
        DynamicBuffer<EntityPrefab> entityReferencesBuffer = SystemAPI.GetBuffer<EntityPrefab>(
            SystemAPI.GetSingletonEntity<EntityPrefabsRegistry>());

        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
        NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.Temp);

        EntityCommandBuffer ecb =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<Spawner> spawner)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<Spawner>>())
        {
            //IDEA: Refactor into corroutines
            // Spawn interval timer
            spawner.ValueRW.spawnPhaseTime -= SystemAPI.Time.DeltaTime;
            if (spawner.ValueRW.spawnPhaseTime <= 0)
            {
                spawner.ValueRW.spawnPhaseTime = spawner.ValueRW.spawnFrequency;

                //Retrieve prefab entity from EntityReferenceKey
                Entity prefabEntity = DataLookup.GetEntityPrefab(ref entityReferencesBuffer, spawner.ValueRO.spawnedEntityKey);

                //Ready up physics query
                distanceHitList.Clear();
                CollisionFilter collisionFilter = new CollisionFilter
                {
                    BelongsTo = ~0u, //All layers
                    CollidesWith = 1u << GameAssets.UNITS_LAYER,
                    GroupIndex = 0
                };

                //Check for nearby enemies to avoid overflow
                int nearbyEntitiesAmount = 0;
                if (collisionWorld.OverlapSphere(
                        localTransform.ValueRO.Position,
                        spawner.ValueRO.nearbyEntityScanRadius,
                        ref distanceHitList,
                        collisionFilter))
                {
                    foreach (DistanceHit distanceHit in distanceHitList)
                    {
                        if (!EntityUtil.ExistsAndPersists(ref state, distanceHit.Entity))
                        {
                            continue;
                        }
                        else if (SystemAPI.HasComponent<Unit>(distanceHit.Entity))
                        {
                            //If the target has a faction
                            if (SystemAPI.HasComponent<Faction>(distanceHit.Entity))
                            {
                                //If the target faction matches the spawned entity faction
                                if (SystemAPI.GetComponent<Faction>(distanceHit.Entity).factionID ==
                                    SystemAPI.GetComponent<Faction>(prefabEntity).factionID)
                                {
                                    nearbyEntitiesAmount++;
                                }
                            }
                        }
                    }
                }

                //If cap was exceeded stop
                if (nearbyEntitiesAmount >= spawner.ValueRO.nearbyEntityCap)
                {
                    continue;
                }

                //Generate spawn
                Entity entity = state.EntityManager.Instantiate(prefabEntity);
                SystemAPI.SetComponent(entity, LocalTransform.FromPosition(localTransform.ValueRO.Position));

                ecb.AddComponent(entity, new RandomWalk
                {
                    originPointPosition = localTransform.ValueRO.Position,
                    targetPostion = localTransform.ValueRO.Position,
                    //TODO: Refactor into reference to EntitiesReference (through query)
                    minDistance = spawner.ValueRO.minDistance,
                    maxDistance = spawner.ValueRO.maxDistance,
                    random = new Unity.Mathematics.Random((uint)entity.Index)
                });
            }
        }
    }

    
}
