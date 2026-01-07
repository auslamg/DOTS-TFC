using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

partial struct UnitMoverSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        UnitMoverJob unitMoverJob = new UnitMoverJob
        {
            deltaTime = SystemAPI.Time.DeltaTime
        };
        unitMoverJob.ScheduleParallel();

        //[Deprecated]
        // Single-thread alternative.
        /* 
        foreach ((
            RefRW<LocalTransform> localTransform,
            RefRO<UnitMover> unitMover,
            RefRW<PhysicsVelocity> physicsVelocity)
               in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRO<UnitMover>,
                RefRW<PhysicsVelocity>>())
        {
            //Desired normalized move direction based on positional difference
            float3 moveDirection = unitMover.ValueRO.targetPosition - localTransform.ValueRO.Position;
            moveDirection = math.normalize(moveDirection);

            //Rotate unit towards move direction
            localTransform.ValueRW.Rotation =
                        math.slerp(localTransform.ValueRO.Rotation, quaternion.LookRotation(moveDirection, math.up()), SystemAPI.Time.DeltaTime * unitMover.ValueRO.rotationSpeed);

            //Apply linear velocity and clamp angular (safety measure for constraint failures)
            physicsVelocity.ValueRW.Linear = moveDirection * unitMover.ValueRO.moveSpeed;
            physicsVelocity.ValueRW.Angular = float3.zero;

            //Transform movement alternative:
            //localTransform.ValueRW.Position += moveDirection * unitMover.ValueRO.value * SystemAPI.Time.DeltaTime;
        }
         */
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
    public void Execute(ref LocalTransform localTransform, in UnitMover unitMover, ref PhysicsVelocity physicsVelocity)
    {
        //Desired normalized move direction based on positional difference
        float3 moveDirection = unitMover.targetPosition - localTransform.Position;

        float targetReachedDistanceSquared = unitMover.targetReachedDistanceSquared; //REVIEW: Take in account for melee atacks
        if (math.lengthsq(moveDirection) <= targetReachedDistanceSquared)
        {
            //Reached target
            physicsVelocity.Linear = float3.zero;
            physicsVelocity.Angular = float3.zero;
            return;
        }

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
