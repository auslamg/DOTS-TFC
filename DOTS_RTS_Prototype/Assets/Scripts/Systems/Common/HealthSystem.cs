using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

[UpdateBefore(typeof(GridSystem))]
/// <summary>
/// Applies entity death when health reaches zero and queues structural destruction.
/// </summary>
partial struct HealthSystem : ISystem
{
    /// <summary>
    /// Marks death events and schedules entity destruction through an end-simulation command buffer.
    /// Triggers <see cref="UnitSelectionManager.OnSelectionChange"/> if a selected unit has died.
    /// </summary>
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((
            RefRW<Health> health,
            Entity entity)
                in SystemAPI.Query<
                RefRW<Health>>().
                WithEntityAccess())
        {
            if (health.ValueRO.currentHealth <= 0)
            {
                // Mark dead unit and buffer destruction.
                health.ValueRW.onDeath = true;
                entityCommandBuffer.DestroyEntity(entity);
            }
        }

        foreach ((
            RefRO<Health> health,
            RefRO<Selected> selected)
                in SystemAPI.Query<
                RefRO<Health>,
                RefRO<Selected>>())
        {
            // Selected unit has died, update selection.
            if (health.ValueRO.onDeath)
            {
                DOTSEventManager.Instance.TriggerOnSelectedDeath();
            }
        }
    }
}