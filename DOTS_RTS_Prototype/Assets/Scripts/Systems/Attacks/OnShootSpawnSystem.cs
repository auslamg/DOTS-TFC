using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

/// <summary>
/// Spawns secondary on-shoot effect entities when <see cref="ShootAttack.onShoot"/> is triggered.
/// </summary>
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct ShootLightSpawnerSystem : ISystem
{
    /// <summary>
    /// Requires the prefab registry singleton before this system can run.
    /// </summary>
    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntityPrefabsRegistry>();
    }

    /// <summary>
    /// Spawns on-shoot effect entities for attacks that fired this frame.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Retrieve the prefab registry buffer used to resolve effect entity prefabs
        DynamicBuffer<EntityPrefab> entityReferencesBuffer = SystemAPI.GetBuffer<EntityPrefab>(
            SystemAPI.GetSingletonEntity<EntityPrefabsRegistry>());

        foreach (RefRO<ShootAttack> shootAttack in SystemAPI.Query<RefRO<ShootAttack>>())
        {
            if (shootAttack.ValueRO.onShoot.isTriggered)
            {
                // Resolve the configured effect prefab by the shoot event's entity key
                if (DataLookup.TryGetEntityPrefab(ref entityReferencesBuffer, shootAttack.ValueRO.onShoot.spawnedEntityKey, out Entity prefabEntity))
                {
                    Entity spawnedEntity = state.EntityManager.Instantiate(prefabEntity);
                    SystemAPI.SetComponent(spawnedEntity, LocalTransform.FromPosition(shootAttack.ValueRO.onShoot.shootFromPosition));
                }
            }
        }
    }
}
