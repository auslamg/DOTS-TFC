using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Harvests resources periodically based on harvester component settings.
/// </summary>
partial struct HarvesterSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<Harvester> harvester)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<Harvester>>())
        {
            if (harvester.ValueRW.harvestCooldownTimer.Tick(SystemAPI.Time.DeltaTime))
            {
                ResourceManager.Instance?.AddResourceValue(harvester.ValueRO.harvestedResourceKey, harvester.ValueRO.harvestAmount);
            }
        }
    }
}
