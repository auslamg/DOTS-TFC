using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;

[UpdateInGroup(typeof(LateSimulationSystemGroup), OrderLast = true)]
/// <summary>
/// Resets one-frame gameplay event flags at the end of simulation.
/// </summary>
partial struct ResetEventSystem : ISystem
{
    private NativeArray<JobHandle> jobHandleArray;

    /// <summary>
    /// Allocates the fixed-size job handle array used to combine reset job dependencies.
    /// </summary>
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        jobHandleArray = new NativeArray<JobHandle>(4, Allocator.Persistent);
    }
    
    /// <summary>
    /// Schedules all event reset jobs and triggers managed trainer queue notifications.
    /// </summary>
    public void OnUpdate(ref SystemState state)
    {
        // Schedule parallel reset jobs to clear all per-entity event flags
        jobHandleArray[0] = new ResetSelectedEventsJob().ScheduleParallel(state.Dependency);
        jobHandleArray[1] = new ResetHealthEventsJob().ScheduleParallel(state.Dependency);
        jobHandleArray[2] = new ResetMeleeAttackEventsJob().ScheduleParallel(state.Dependency);
        jobHandleArray[3] = new ResetShootAttackEventsJob().ScheduleParallel(state.Dependency);

        // Complete trainer-event job synchronously to collect changed entities before managed dispatch
        NativeList<Entity> onTrainerUnitQueueChangeFiredEntities = new NativeList<Entity>(Allocator.TempJob);
        new ResetTrainerEventsJob
        {
            onUnitQueueChangeFiredEntities = onTrainerUnitQueueChangeFiredEntities.AsParallelWriter(),
        }.ScheduleParallel(state.Dependency).Complete();

        //REVIEW: Managed code so no Burst, although the code on this method is fairly costless (there's only Job schedulling)
        DOTSEventManager.Instance.TriggerOnTrainerUnitQueueChange(onTrainerUnitQueueChangeFiredEntities);

        // Combine all parallel job handles so downstream systems wait on all reset jobs
        state.Dependency = JobHandle.CombineDependencies(jobHandleArray);
    }

}
[BurstCompile]
[WithPresent(typeof(Selected))]
/// <summary>
/// Clears selection event flags after consumers have processed them.
/// </summary>
public partial struct ResetSelectedEventsJob : IJobEntity
{
    /// <summary>
    /// Resets selected and deselected one-frame flags.
    /// </summary>
    public void Execute(ref Selected selected)
    {
        selected.onSelected = false;
        selected.onDeselected = false;
    }
}

[BurstCompile]
/// <summary>
/// Clears health event flags after consumers have processed them.
/// </summary>
public partial struct ResetHealthEventsJob : IJobEntity
{
    /// <summary>
    /// Resets health-changed and death one-frame flags.
    /// </summary>
    public void Execute(ref Health health)
    {
        health.onHealthChanged = false;
        health.onDeath = false;
    }
}

[BurstCompile]
/// <summary>
/// Clears melee attack trigger flags after consumers have processed them.
/// </summary>
public partial struct ResetMeleeAttackEventsJob : IJobEntity
{
    /// <summary>
    /// Resets the melee attack one-frame event flag.
    /// </summary>
    public void Execute(ref MeleeAttack meleeAttack)
    {
        meleeAttack.onAttack = false;
    }
}

[BurstCompile]
/// <summary>
/// Clears shoot trigger flags after consumers have processed them.
/// </summary>
public partial struct ResetShootAttackEventsJob : IJobEntity
{
    /// <summary>
    /// Resets the ranged attack one-frame trigger event.
    /// </summary>
    public void Execute(ref ShootAttack shootAttack)
    {
        shootAttack.onShoot.isTriggered = false;
    }
}

[BurstCompile]
/// <summary>
/// Clears trainer queue-change flags while collecting entities that fired queue events.
/// </summary>
public partial struct ResetTrainerEventsJob : IJobEntity
{
    public NativeList<Entity>.ParallelWriter onUnitQueueChangeFiredEntities;

    /// <summary>
    /// Enqueues trainer entities that changed queue state before resetting their event flag.
    /// </summary>
    public void Execute(ref Trainer trainer, Entity entity)
    {
        if (trainer.onUnitQueueChange)
        {
            onUnitQueueChangeFiredEntities.AddNoResize(entity);
        }
        trainer.onUnitQueueChange = false;
    }
}