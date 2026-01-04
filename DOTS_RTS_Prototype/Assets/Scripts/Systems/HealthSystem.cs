using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

partial struct HealthSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        //Alternative new EntityCommandBuffer (unoptimized)
        /* new EntityCommandBuffer(Allocator.Temp); */

        foreach ((
            RefRO<Health> unitHealth,
            Entity entity)
                in SystemAPI.Query<
                RefRO<Health>>().WithEntityAccess())
        {
            if (unitHealth.ValueRO.currentHealth <= 0)
            {
                //Dead unit
                entityCommandBuffer.DestroyEntity(entity);

                //[Deprecated]
                //Cannot destroy entities at runtime due to structural change errors.
                //Buffer the operation for future entity destruction. Manual destruction:
                /* state.EntityManager.DestroyEntity(entity); */

            }
        }
        //[Deprecated]
        // New EntityCommandBuffers have to be played (.Playback) to commit structural changes. 
        /* entityCommandBuffer.Playback(state.EntityManager); */
    }
}
