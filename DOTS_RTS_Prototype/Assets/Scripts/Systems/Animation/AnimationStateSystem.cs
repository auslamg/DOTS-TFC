using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
[UpdateAfter(typeof(ShootAttackSystem))]
/// <summary>
/// Resolves high-level gameplay state into requested animation keys.
/// </summary>
partial struct AnimationStateSystem : ISystem
{
    public ComponentLookup<ActiveAnimation> activeAnimationComponentLookup;

    /// <summary>
    /// Initializes lookup access to mesh animation state components.
    /// </summary>
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        activeAnimationComponentLookup = state.GetComponentLookup<ActiveAnimation>(false);
    }

    /// <summary>
    /// Updates lookups and schedules state jobs for movement, aiming, and melee transitions.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        activeAnimationComponentLookup.Update(ref state);

        IdleWalkingAnimationStateJob job = new IdleWalkingAnimationStateJob
        {
            activeAnimationComponentLookup = activeAnimationComponentLookup
        };
        job.ScheduleParallel(); //TODO: Rename

        AimShootAnimationStateJob job2 = new AimShootAnimationStateJob
        {
            activeAnimationComponentLookup = activeAnimationComponentLookup
        };
        job2.ScheduleParallel(); //TODO: Rename

        MeleeAttackAnimationStateJob job3 = new MeleeAttackAnimationStateJob
        {
            activeAnimationComponentLookup = activeAnimationComponentLookup
        };
        job3.ScheduleParallel(); //TODO: Rename
    }
}

/// <summary>
/// Applies idle or walk animation requests based on current movement state.
/// </summary>
public partial struct IdleWalkingAnimationStateJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public ComponentLookup<ActiveAnimation> activeAnimationComponentLookup;

    /// <summary>
    /// Sets the next animation key to walk while moving, otherwise idle.
    /// </summary>
    public void Execute(in AnimatedMeshReference animatedMesh,
            in UnitMover unitMover,
            in UnitAnimations unitAnimations)
    {
        RefRW<ActiveAnimation> activeAnimation = activeAnimationComponentLookup.GetRefRW(animatedMesh.meshEntity);

        //TODO: Refactor into fsm
        if (unitMover.isMoving)
        {
            activeAnimation.ValueRW.nextAnimationKey = unitAnimations.walkAnimationKey;
        }
        else
        {
            activeAnimation.ValueRW.nextAnimationKey = unitAnimations.idleAnimationKey;
        }
    }
}

/// <summary>
/// Applies aiming and shooting animation requests for ranged units.
/// </summary>
public partial struct AimShootAnimationStateJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public ComponentLookup<ActiveAnimation> activeAnimationComponentLookup;

    /// <summary>
    /// Chooses aim when locked on target and shoot when a shot trigger event fires.
    /// </summary>
    public void Execute(in AnimatedMeshReference animatedMesh,
            in UnitMover unitMover,
            in Targetter targetter,
            in ShootAttack shootAttack,
            in UnitAnimations unitAnimations)
    {
        //FIX: Use utils method
        //TODO: Refactor into fsm
        if (!unitMover.isMoving && targetter.targetEntity != Entity.Null)
        {
            RefRW<ActiveAnimation> activeAnimation =
                activeAnimationComponentLookup.GetRefRW(animatedMesh.meshEntity);
            activeAnimation.ValueRW.nextAnimationKey = unitAnimations.aimAnimationKey;
        }


        //TODO: Refactor into fsm
        if (shootAttack.onShoot.isTriggered)
        {
            RefRW<ActiveAnimation> activeAnimation =
                activeAnimationComponentLookup.GetRefRW(animatedMesh.meshEntity);
            activeAnimation.ValueRW.nextAnimationKey = unitAnimations.shootAnimationKey;
        }
    }
}

/// <summary>
/// Applies melee attack animation requests when attack events are raised.
/// </summary>
public partial struct MeleeAttackAnimationStateJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public ComponentLookup<ActiveAnimation> activeAnimationComponentLookup;

    /// <summary>
    /// Switches to the melee attack clip for the current frame when attack is triggered.
    /// </summary>
    public void Execute(in AnimatedMeshReference animatedMesh,
            in MeleeAttack meleeAttack,
            in UnitAnimations unitAnimations)
    {
        //TODO: Refactor into fsm
        if (meleeAttack.onAttack)
        {
            RefRW<ActiveAnimation> activeAnimation = activeAnimationComponentLookup.GetRefRW(animatedMesh.meshEntity);
            
            activeAnimation.ValueRW.nextAnimationKey = unitAnimations.meleeAttackAnimationKey;
        }
    }
}
