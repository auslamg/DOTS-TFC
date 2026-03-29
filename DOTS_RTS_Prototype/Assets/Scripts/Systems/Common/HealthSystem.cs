using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

/// <summary>
/// Applies entity death when health reaches zero and queues structural destruction.
/// </summary>
partial struct HealthSystem : ISystem
{
    /// <summary>
    /// Marks death events and schedules entity destruction through an end-simulation command buffer.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        //Alternative new EntityCommandBuffer (unoptimized)
        /* new EntityCommandBuffer(Allocator.Temp); */

        foreach ((
            RefRW<Health> health,
            Entity entity)
                in SystemAPI.Query<
                RefRW<Health>>().
                WithEntityAccess())
        {
            if (health.ValueRO.currentHealth <= 0)
            {
                //Dead unit
                health.ValueRW.onDeath = true;
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
