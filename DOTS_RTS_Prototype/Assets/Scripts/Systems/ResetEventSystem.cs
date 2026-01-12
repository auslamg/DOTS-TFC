using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;

[UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
partial struct ResetEventSystem : ISystem
{
    private NativeArray<JobHandle> jobHandleArray;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        jobHandleArray = new NativeArray<JobHandle>(4, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        jobHandleArray[0] = new ResetSelectedEventsJob().ScheduleParallel(state.Dependency);
        jobHandleArray[1] = new ResetHealthEventsJob().ScheduleParallel(state.Dependency);
        jobHandleArray[2] = new ResetMeleeAttackEventsJob().ScheduleParallel(state.Dependency);
        jobHandleArray[3] = new ResetShootAttackEventsJob().ScheduleParallel(state.Dependency);

        state.Dependency = JobHandle.CombineDependencies(jobHandleArray);
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