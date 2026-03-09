using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct ShootLightSpawnerSystem : ISystem
{
    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntityPrefabsRegistry>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Used for prefab instancing
        DynamicBuffer<EntityReference> entityReferencesBuffer = SystemAPI.GetBuffer<EntityReference>(
            SystemAPI.GetSingletonEntity<EntityPrefabsRegistry>());

        EntityPrefabsRegistry entitiesReferences = SystemAPI.GetSingleton<EntityPrefabsRegistry>();
        foreach (RefRO<ShootAttack> shootAttack in SystemAPI.Query<RefRO<ShootAttack>>())
        {
            if (shootAttack.ValueRO.onShoot.isTriggered)
            {
                if (RegistryAccessor.TryGetPrefabEntity(ref entityReferencesBuffer, shootAttack.ValueRO.onShoot.spawnedEntityKey, out Entity prefabEntity))
                {
                    Entity spawnedEntity = state.EntityManager.Instantiate(prefabEntity);
                    SystemAPI.SetComponent(spawnedEntity, LocalTransform.FromPosition(shootAttack.ValueRO.onShoot.shootFromPosition));
                }
            }
        }
    }
}
