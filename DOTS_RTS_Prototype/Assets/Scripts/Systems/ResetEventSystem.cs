using Unity.Burst;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct ResetEventSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (RefRW<Selected> selected in SystemAPI.Query<RefRW<Selected>>().WithPresent<Selected>())
        {
            selected.ValueRW.onSelected = false;
            selected.ValueRW.onDeselected = false;
        }
    }

}
