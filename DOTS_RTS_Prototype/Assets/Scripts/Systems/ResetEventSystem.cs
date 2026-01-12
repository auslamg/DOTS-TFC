using Unity.Burst;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;

[UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
partial struct ResetEventSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new ResetSelectedEventsJob().ScheduleParallel(state.Dependency);
        new ResetHealthEventsJob().ScheduleParallel(state.Dependency);
        new ResetMeleeAttackEventsJob().ScheduleParallel(state.Dependency);
        new ResetShootAttackEventsJob().ScheduleParallel(state.Dependency);
    }

}
[BurstCompile]
[WithPresent(typeof(Selected))]
public partial struct ResetSelectedEventsJob : IJobEntity
{
    public void Execute(ref Selected selected)
    {
        selected.onSelected = false;
        selected.onDeselected = false;
    }
}

[BurstCompile]
public partial struct ResetHealthEventsJob : IJobEntity
{
    public void Execute(ref Health health)
    {
        health.onHealthChanged = false;
    }
}

[BurstCompile]
public partial struct ResetMeleeAttackEventsJob : IJobEntity
{
    public void Execute(ref MeleeAttack meleeAttack)
    {
        meleeAttack.onAttack = false;
    }
}

[BurstCompile]
public partial struct ResetShootAttackEventsJob : IJobEntity
{
    public void Execute(ref ShootAttack shootAttack)
    {
        shootAttack.onShoot.isTriggered = false;
    }
}