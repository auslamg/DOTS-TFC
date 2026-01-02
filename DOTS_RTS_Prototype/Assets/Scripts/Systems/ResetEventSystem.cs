using Unity.Burst;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;

[UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
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

        foreach (RefRW<Health> health in SystemAPI.Query<RefRW<Health>>())
        {
            health.ValueRW.onHealthChanged = false;
        }

        foreach (RefRW<ShootAttack> shootAttack in SystemAPI.Query<RefRW<ShootAttack>>())
        {
            shootAttack.ValueRW.onShoot.isTriggered = false;
        }
    }

}
