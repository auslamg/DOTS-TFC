using Unity.Burst;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Destroys entities once their <see cref="SelfDestroy.delay"/> timer expires.
/// </summary>
partial struct TestStructsSystem : ISystem
{
    /// <summary>
    /// Decrements self-destroy timers and queues expired entities for destruction.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRW<TestStructs> testStructs,
            Entity entity)
                in SystemAPI.Query<
                RefRW<TestStructs>>().
                WithEntityAccess())
        {
            if (testStructs.ValueRW.tickTimer.Tick(SystemAPI.Time.DeltaTime))
            {
                Debug.Log("Tick");
            } 
        }
    }
}
