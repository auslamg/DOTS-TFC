using Unity.Burst;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Logs periodic ticks for testing purposes.
/// </summary>
partial struct TestStructsSystem : ISystem
{
    /// <summary>
    /// Logs ticks when test timers expire.
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
