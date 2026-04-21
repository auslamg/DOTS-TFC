using System.Globalization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static GridSystem;

/// <summary>
/// Schedules movement simulation for units based on their current target positions.
/// </summary>
partial struct UnitMoverSystem : ISystem
{
    /// <summary>
    /// Job handles for the parallel reset jobs. This array is allocated once and reused across updates.
    /// </summary>
    private NativeArray<JobHandle> jobHandleArray;

    /// <summary>
    /// Requires the grid data registry singleton before this system can run.
    /// </summary>
    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridData>();
        jobHandleArray = new NativeArray<JobHandle>(1, Allocator.Persistent);

    }

    /// <summary>
    /// Returns true when the supplied grid cell represents an obstructed cell.
    /// </summary> //TODO Implement
    /* public static bool IsPathable(ref SystemState state, float3 worldPosition, int flowFieldIndex, GridData gridData)
    {
        int2 coords = WorldPositionToCoords(worldPosition, gridData.gridCellSize);
        int cellIndex = CoordsToIndex(coords, gridData.width);
        Entity cell = gridData.flowFieldArray[flowFieldIndex].gridCellEntityArray[cellIndex];

        GridCell gridCell;

        return ValidateCoords(coords, gridData) && gridCell.bestPathCost < UNPATHABLE_COST; // && this cell.bestPathCost < UNPATHABLE_COST
    } */

    /// <summary>
    /// Schedules the movement job that updates velocity, facing, and movement state.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        jobHandleArray[0] = new StartTargetPositionJob().ScheduleParallel(state.Dependency);

        state.Dependency = JobHandle.CombineDependencies(jobHandleArray);
        jobHandleArray[0].Complete();

        GridData gridData = SystemAPI.GetSingleton<GridData>();

        CollisionWorld collisionWorld = state.EntityManager.GetCollisionWorld();

        foreach ((
                RefRO<LocalTransform> localTransform,
                RefRW<UnitMover> unitMover,
                RefRW<PathRequest> pathRequest,
                EnabledRefRW<PathRequest> pathRequestEnabled,
                RefRW<FlowFieldPathRequest> flowFieldPathRequest,
                EnabledRefRW<FlowFieldPathRequest> flowFieldPathRequestEnabled,
                EnabledRefRW<FlowFieldFollower> flowFieldFollowerEnabled,
                Entity entity)
                    in SystemAPI.Query<
                    RefRO<LocalTransform>,
                    RefRW<UnitMover>,
                    RefRW<PathRequest>,
                    EnabledRefRW<PathRequest>,
                    RefRW<FlowFieldPathRequest>,
                    EnabledRefRW<FlowFieldPathRequest>,
                    EnabledRefRW<FlowFieldFollower>>().
                    WithPresent<FlowFieldPathRequest>().
                    WithPresent<FlowFieldFollower>().
                    WithEntityAccess())
        {
            //Check if a straight path to  target is available. If not, request navigation.
            RaycastInput raycastInput = new RaycastInput
            {
                Start = localTransform.ValueRO.Position,
                End = pathRequest.ValueRO.targetPosition,
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.OBSTRUCTION_LAYER,
                    GroupIndex = 0
                }
            };
            // Hit nothing.
            if (!collisionWorld.CastRay(raycastInput))
            {
                Debug.Log("Pathfinding: Moving straight");
                unitMover.ValueRW.targetPosition = pathRequest.ValueRO.targetPosition;
                flowFieldPathRequestEnabled.ValueRW = false;
                flowFieldFollowerEnabled.ValueRW = false;
            }
            // Obstructed path, might require navigation.
            else
            {
                // FIX This causes a bug where currently attacking units cannot be moved
                if (SystemAPI.HasComponent<ManualMove>(entity))
                {
                    SystemAPI.SetComponentEnabled<ManualMove>(entity, false);
                }

                // Walkable AND pathable, ask for navigation
                if (GridSystem.IsWalkable(pathRequest.ValueRO.targetPosition, gridData))
                {
                    Debug.Log("Pathfinding: Navigating to WALKABLE");
                    flowFieldPathRequest.ValueRW.targetPosition = pathRequest.ValueRO.targetPosition;
                    flowFieldPathRequestEnabled.ValueRW = true;
                }
                // Unwalkable position, simply don't navigate.
                else
                {
                    Debug.Log("Pathfinding: NOT WALKABLE");
                    unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;
                    flowFieldPathRequestEnabled.ValueRW = false;
                    flowFieldFollowerEnabled.ValueRW = false;
                }

                /// [Deprecated]: Unreachable path calculation. Rather than doing all this complex calculations,
                /// units just check wether the current cell has been written to or not.
                /* if (FlowFieldExists(unitMover.ValueRW.targetPosition, gridData, out FlowField flowField))
                {
                    if (!IsPathable(pathRequest.ValueRW.targetPosition, flowField, gridData, ref state))
                    {
                        Debug.LogError("Pathfinding: UNREACHABLE");
                        unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;
                        flowFieldPathRequestEnabled.ValueRW = false;
                        flowFieldFollowerEnabled.ValueRW = false;
                    }
                } */
            }

            pathRequestEnabled.ValueRW = false;
        }


        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<FlowFieldFollower> flowFieldFollower,
            EnabledRefRW<FlowFieldFollower> flowFieldFollowerEnabled,
            RefRW<UnitMover> unitMover)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<FlowFieldFollower>,
                EnabledRefRW<FlowFieldFollower>,
                RefRW<UnitMover>>())
        {
            // Retrieve current grid cell's pathing vector and convert it to world space
            int2 gridPosition = GridSystem.WorldPositionToCoords(localTransform.ValueRO.Position, gridData.gridCellSize);
            int currentCellIndex = GridSystem.CoordsToIndex(gridPosition, gridData.width);
            Entity currentCell = gridData.flowFieldArray[flowFieldFollower.ValueRO.flowFieldIndex].gridCellEntityArray[currentCellIndex];
            GridCell gridCell = SystemAPI.GetComponent<GridCell>(currentCell);
            float3 worldMovementVector = GridSystem.GridVectorToWorldSpace(gridCell.pathingVector);

            // If inside a wall, use the previous cell's vector. Else, read cell vector.
            if (GridSystem.IsObstructed(gridCell))
            {
                worldMovementVector = flowFieldFollower.ValueRO.lastMoveVector;
            }
            else
            {
                flowFieldFollower.ValueRW.lastMoveVector = worldMovementVector;
            }

            // No path was found, stop movement.
            if (!GridSystem.IsPathable(gridCell) &&
                !GridSystem.IsObstructed(gridCell))
            {
                Debug.Log("Pathfinding: UNREACHABLE");
                worldMovementVector = float3.zero;
                unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;
                continue;
            }

            unitMover.ValueRW.targetPosition =
                GridSystem.CoordsToWorldPositionCenter(gridPosition, gridData.gridCellSize) +
                worldMovementVector * gridData.gridCellSize * 2;

            // Detect if the unit has reached its destination
            if (math.distance(localTransform.ValueRO.Position, flowFieldFollower.ValueRO.targetPosition) < gridData.gridCellSize * 1.5f)
            {
                Debug.Log("Stopped unit");
                unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;
                flowFieldFollowerEnabled.ValueRW = false;
            }

            //Check if a straight path to  target is available.
            RaycastInput raycastInput = new RaycastInput
            {
                Start = localTransform.ValueRO.Position,
                End = flowFieldFollower.ValueRO.targetPosition,
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.OBSTRUCTION_LAYER,
                    GroupIndex = 0
                }
            };
            if (!collisionWorld.CastRay(raycastInput))
            {
                // Hit nothing. Take a straight path
                unitMover.ValueRW.targetPosition = flowFieldFollower.ValueRO.targetPosition;
                flowFieldFollowerEnabled.ValueRW = false;
            }
        }

        MoveUnitJob moveUnitJob = new MoveUnitJob
        {
            deltaTime = SystemAPI.Time.DeltaTime
        };
        moveUnitJob.ScheduleParallel();
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
public partial struct MoveUnitJob : IJobEntity
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
