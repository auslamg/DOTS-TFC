using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;

partial struct EnemySpawnerSystem : ISystem
{
    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntityPrefabsRegistry>();
    }


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityPrefabsRegistry entitiesReferences = SystemAPI.GetSingleton<EntityPrefabsRegistry>();
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;
        NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.Temp);

        EntityCommandBuffer entityCommandBuffer =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<EnemySpawner> enemySpawner)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<EnemySpawner>>())
        {
            //IDEA: Refactor into corroutines
            //Timer
            enemySpawner.ValueRW.spawnPhaseTime -= SystemAPI.Time.DeltaTime;
            if (enemySpawner.ValueRW.spawnPhaseTime <= 0)
            {
                enemySpawner.ValueRW.spawnPhaseTime = enemySpawner.ValueRW.spawnFrequency;

                //Ready up physics query
                distanceHitList.Clear();
                CollisionFilter collisionFilter = new CollisionFilter
                {
                    BelongsTo = ~0u, //All layers
                    CollidesWith = 1u << GameAssets.UNITS_LAYER,
                    GroupIndex = 0
                };

                //Check for nearby enemies to avoid overflow
                int nearbyEnemiesAmount = 0;
                if (collisionWorld.OverlapSphere(
                    localTransform.ValueRO.Position,
                    enemySpawner.ValueRO.nearbyEnemyScanRadius,
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
                                //TODO: Parametrize
                                //If the target faction matches the spawned entity faction
                                if (SystemAPI.GetComponent<Faction>(distanceHit.Entity).factionID ==
                                    SystemAPI.GetComponent<Faction>(entitiesReferences.enemyPrefabEntity).factionID)
                                {
                                    nearbyEnemiesAmount++;
                                }
                            }
                        }
                    }
                }

                //If cap was exceeded stop
                if (nearbyEnemiesAmount >= enemySpawner.ValueRO.nearbyEnemyCap)
                {
                    continue;
                }

                //Generate spawn
                //TODO: Parametrize
                Entity enemyEntity = state.EntityManager.Instantiate(entitiesReferences.enemyPrefabEntity);
                SystemAPI.SetComponent(enemyEntity, LocalTransform.FromPosition(localTransform.ValueRO.Position));

                entityCommandBuffer.AddComponent(enemyEntity, new RandomWalk
                {
                    originPointPosition = localTransform.ValueRO.Position,
                    targetPostion = localTransform.ValueRO.Position,
                    //TODO: Refactor into reference to EntitiesReference (through query)
                    minDistance = enemySpawner.ValueRO.minDistance,
                    maxDistance = enemySpawner.ValueRO.maxDistance,
                    random = new Unity.Mathematics.Random((uint)enemyEntity.Index)
                });
            }
        }
    }
}
