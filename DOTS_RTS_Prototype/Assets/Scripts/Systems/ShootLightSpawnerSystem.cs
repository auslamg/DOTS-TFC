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
        EntityPrefabsRegistry entitiesReferences = SystemAPI.GetSingleton<EntityPrefabsRegistry>();
        foreach (RefRO<ShootAttack> shootAttack in SystemAPI.Query<RefRO<ShootAttack>>())
        {
            if (shootAttack.ValueRO.onShoot.isTriggered)
            {
                Entity shootLight = state.EntityManager.Instantiate(entitiesReferences.shootLightPrefabEntity);
                SystemAPI.SetComponent(shootLight, LocalTransform.FromPosition(shootAttack.ValueRO.onShoot.shootFromPosition));
            }

        }
    }
}
