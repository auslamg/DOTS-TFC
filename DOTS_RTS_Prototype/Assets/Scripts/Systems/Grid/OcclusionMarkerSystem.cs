using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

/// <summary>
/// Marks occluder components for removal when their owning entities die.
/// </summary>
[UpdateAfter(typeof(HealthSystem))]
[UpdateBefore(typeof(GridSystem))]
partial struct OcclusionMarkerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
           RefRW<Occluder> occluder,
           RefRO<Health> health)
               in SystemAPI.Query<
               RefRW<Occluder>,
               RefRO<Health>>())
        {
            if (health.ValueRO.onDeath)
            {
                occluder.ValueRW.markedForDeletion = true;
                occluder.ValueRW.isAccountedFor = false;
            }
        }
    }
}
