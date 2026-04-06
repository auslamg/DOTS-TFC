using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Schedules movement simulation for units based on their current target positions.
/// </summary>
partial struct UnitMoverSystem : ISystem
{
    /// <summary>
    /// Schedules the movement job that updates velocity, facing, and movement state.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        StartTargetPositionJob startTargetPositionJob = new StartTargetPositionJob {};
        startTargetPositionJob.ScheduleParallel();

        UnitMoverJob unitMoverJob = new UnitMoverJob
        {
            deltaTime = SystemAPI.Time.DeltaTime
        };
        unitMoverJob.ScheduleParallel();
    }
}

/// <summary>
/// Resets unit target position if there is none after spawning to avoid the unit going to the default value (0,0,0) 
/// </summary>
[BurstCompile]
public partial struct StartTargetPositionJob : IJobEntity
{
    public void Execute(in LocalTransform localTransform, ref UnitMover unitMover)
    {
        if (unitMover.hasStartedTargetPosition)
        {
            return;
        }
        unitMover.hasStartedTargetPosition = true;

        if (math.lengthsq(unitMover.targetPosition) == 0f)
        {
            unitMover.targetPosition = localTransform.Position;
        }
    }
}

/// <summary>
/// Moves a unit towards its target position and adjusts the rotation to match the movement direction.
/// </summary>
[BurstCompile]
public partial struct UnitMoverJob : IJobEntity
{
    //Set on struct construction
    public float deltaTime;

    /// <summary>
    /// Moves a unit toward its target and stops movement when the reach threshold is satisfied.
    /// </summary>
    public void Execute(ref LocalTransform localTransform, ref UnitMover unitMover, ref PhysicsVelocity physicsVelocity)
    {
        //Desired normalized move direction based on positional difference
        float3 moveDirection = unitMover.targetPosition - localTransform.Position;

        float targetReachedDistanceSquared = unitMover.targetReachedDistanceSquared; //REVIEW: Take in account for melee atacks
        if (math.lengthsq(moveDirection) <= targetReachedDistanceSquared)
        {
            //Reached target
            physicsVelocity.Linear = float3.zero;
            physicsVelocity.Angular = float3.zero;
            unitMover.isMoving = false;
            return;
        }
        unitMover.isMoving = true;


        moveDirection = math.normalize(moveDirection);

        //Rotate unit towards move direction
        localTransform.Rotation =
                    math.slerp(localTransform.Rotation, quaternion.LookRotation(moveDirection, math.up()), deltaTime * unitMover.rotationSpeed);

        //Apply linear velocity and clamp angular (safety measure for constraint failures)
        physicsVelocity.Linear = moveDirection * unitMover.moveSpeed;
        physicsVelocity.Angular = float3.zero;

        //Transform movement alternative:
        //localTransform.ValueRW.Position += moveDirection * unitMover.ValueRO.value * SystemAPI.Time.DeltaTime;
    }
}
