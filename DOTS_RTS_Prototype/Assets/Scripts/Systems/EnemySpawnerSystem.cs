using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

partial struct EnemySpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Used for prefab instancing
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();

        foreach ((RefRO<LocalTransform> localTransform,
                  RefRW<EnemySpawner> enemySpawner)
                    in SystemAPI.Query<
                    RefRO<LocalTransform>,
                    RefRW<EnemySpawner>>())
        {
            //IDEA: Refactor into corroutines
            //FIX: Avoid continue. Maybe labels/goto?
            enemySpawner.ValueRW.spawnPhaseTime -= SystemAPI.Time.DeltaTime;
            if (enemySpawner.ValueRW.spawnPhaseTime > 0)
            {
                continue;
            }
            enemySpawner.ValueRW.spawnPhaseTime = enemySpawner.ValueRW.spawnFrequency;

            Entity enemyEntity = state.EntityManager.Instantiate(entitiesReferences.enemyPrefabEntity);
            SystemAPI.SetComponent(enemyEntity, LocalTransform.FromPosition(localTransform.ValueRO.Position));
        }        
    }
}
