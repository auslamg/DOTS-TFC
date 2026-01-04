using Unity.Burst;
using Unity.Entities;

partial struct SelfDestroySystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((
            RefRW<SelfDestroy> selfDestroy,
            Entity entity)
                in SystemAPI.Query<
                RefRW<SelfDestroy>>().
                WithEntityAccess())
        {
            selfDestroy.ValueRW.delay -= SystemAPI.Time.DeltaTime;
            if (selfDestroy.ValueRO.delay <= 0)
            {
                entityCommandBuffer.DestroyEntity(entity);
            }
        }

    }
}
