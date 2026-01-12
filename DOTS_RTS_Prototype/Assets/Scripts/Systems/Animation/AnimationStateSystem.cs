using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
[UpdateAfter(typeof(ShootAttackSystem))]
partial struct AnimationStateSystem : ISystem
{
    public ComponentLookup<ActiveAnimation> activeAnimationComponentLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        activeAnimationComponentLookup = state.GetComponentLookup<ActiveAnimation>(false);
    }

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

public partial struct IdleWalkingAnimationStateJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public ComponentLookup<ActiveAnimation> activeAnimationComponentLookup;
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

public partial struct AimShootAnimationStateJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public ComponentLookup<ActiveAnimation> activeAnimationComponentLookup;
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

public partial struct MeleeAttackAnimationStateJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public ComponentLookup<ActiveAnimation> activeAnimationComponentLookup;
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
