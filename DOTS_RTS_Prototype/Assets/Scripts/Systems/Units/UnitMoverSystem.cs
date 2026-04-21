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

    public ComponentLookup<PathRequest> pathRequestComponentLookup;
    public ComponentLookup<FlowFieldFollower> flowFieldFollowerComponentLookup;
    public ComponentLookup<FlowFieldRequest> flowFieldRequestComponentLookup;
    public ComponentLookup<ManualMove> manualMoveComponentLookup;
    public ComponentLookup<GridCell> gridCellComponentLookup;


    /// <summary>
    /// Requires the grid data registry singleton before this system can run.
    /// </summary>
    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridData>();
        state.RequireForUpdate<ManualMove>();
        jobHandleArray = new NativeArray<JobHandle>(1, Allocator.Persistent);

        pathRequestComponentLookup = SystemAPI.GetComponentLookup<PathRequest>(isReadOnly: false);
        flowFieldFollowerComponentLookup = SystemAPI.GetComponentLookup<FlowFieldFollower>(isReadOnly: false);
        flowFieldRequestComponentLookup = SystemAPI.GetComponentLookup<FlowFieldRequest>(isReadOnly: false);
        manualMoveComponentLookup = SystemAPI.GetComponentLookup<ManualMove>(isReadOnly: false);
        gridCellComponentLookup = SystemAPI.GetComponentLookup<GridCell>(isReadOnly: false);
    }


    /// <summary>
    /// Schedules the movement job that updates velocity, facing, and movement state.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Get data for jobs.
        GridData gridData = SystemAPI.GetSingleton<GridData>();
        CollisionWorld collisionWorld = state.EntityManager.GetCollisionWorld();

        // Update lookups to get latest frame data.
        pathRequestComponentLookup.Update(ref state);
        flowFieldFollowerComponentLookup.Update(ref state);
        flowFieldRequestComponentLookup.Update(ref state);
        manualMoveComponentLookup.Update(ref state);
        gridCellComponentLookup.Update(ref state);

        // Initialize target position so that units don't go to (0,0,0) world position by default.
        InitializeTargetPositionJob initializeTargetPositionJob = new InitializeTargetPositionJob();
        initializeTargetPositionJob.ScheduleParallel();

        // Handle path requests.
        PathRequestJob pathRequestJob = new PathRequestJob
        {
            pathRequestComponentLookup = pathRequestComponentLookup,
            flowFieldFollowerComponentLookup = flowFieldFollowerComponentLookup,
            flowFieldRequestComponentLookup = flowFieldRequestComponentLookup,
            manualMoveComponentLookup = manualMoveComponentLookup,
            pathingCostMap = gridData.pathingCostMap,
            collisionWorld = collisionWorld,
            gridWidth = gridData.width,
            gridHeight = gridData.height,
            gridCellSize = gridData.gridCellSize
        };
        pathRequestJob.ScheduleParallel();

        // Check if a straight path to target is available to skip navigation.
        CheckStraightPathJob checkStraightPathJob = new CheckStraightPathJob
        {
            flowFieldFollowerComponentLookup = flowFieldFollowerComponentLookup,
            collisionWorld = collisionWorld
        };
        checkStraightPathJob.ScheduleParallel();

        // Follow flow field directions on navigating units.
        FollowFlowFieldJob followFlowFieldJob = new FollowFlowFieldJob
        {
            flowFieldFollowerComponentLookup = flowFieldFollowerComponentLookup,
            gridCellComponentLookup = gridCellComponentLookup,
            globalGridCellIndexedArray = gridData.globalGridCellIndexedArray,
            gridWidth = gridData.width,
            gridHeight = gridData.height,
            gridCellSize = gridData.gridCellSize
        };
        followFlowFieldJob.ScheduleParallel();
        
        // Apply final calculated movement on units.
        MoveUnitJob moveUnitJob = new MoveUnitJob
        {
            deltaTime = SystemAPI.Time.DeltaTime
        };
        moveUnitJob.ScheduleParallel();
    }

    [BurstCompile]
    private void OnDestroy(ref SystemState state)
    {
        jobHandleArray.Dispose();
    }
}

/// <summary>
/// Resets unit target position if there is none after spawning to avoid the unit going to the default value (0,0,0). 
/// </summary>
[BurstCompile]
public partial struct InitializeTargetPositionJob : IJobEntity
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
/// Manages unit pathfinding requests.
/// </summary>
[BurstCompile]
[WithAll(typeof(PathRequest))]
public partial struct PathRequestJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public ComponentLookup<PathRequest> pathRequestComponentLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<FlowFieldFollower> flowFieldFollowerComponentLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<FlowFieldRequest> flowFieldRequestComponentLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<ManualMove> manualMoveComponentLookup;

    /// <summary>Cell pathing cost map inside <see cref="GridData"/>.</summary>
    [ReadOnly] public NativeArray<byte> pathingCostMap;

    /// <summary>Used for physics queries.</summary>
    [ReadOnly] public CollisionWorld collisionWorld;

    /// <summary><see cref="GridData"/> decomposed data to avoid nested collection usage.</summary>
    [ReadOnly] public int gridWidth;

    /// <summary><see cref="GridData"/> decomposed data to avoid nested collection usage.</summary>
    [ReadOnly] public int gridHeight;

    /// <summary><see cref="GridData"/> decomposed data to avoid nested collection usage.</summary>
    [ReadOnly] public float gridCellSize;


    public void Execute(
        in LocalTransform localTransform,
        ref UnitMover unitMover,
        Entity entity)
    {
        // Lookup local fetch for readability.
        PathRequest pathRequest = pathRequestComponentLookup[entity];

        //Check if a straight path to  target is available. If not, request navigation.
        RaycastInput raycastInput = new RaycastInput
        {
            Start = localTransform.Position,
            End = pathRequest.targetPosition,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.OBSTRUCTION_LAYER,
                GroupIndex = 0
            }
        };
        if (!collisionWorld.CastRay(raycastInput))
        {
            // Hit nothing: moving straight towards target.
            unitMover.targetPosition = pathRequest.targetPosition;
            flowFieldRequestComponentLookup.SetComponentEnabled(entity, false);
            flowFieldFollowerComponentLookup.SetComponentEnabled(entity, false);
        }
        else
        {
            // Obstructed path, might require navigation.

            // FIX This causes a bug where currently attacking units cannot be moved
            if (manualMoveComponentLookup.HasComponent(entity))
            {
                manualMoveComponentLookup.SetComponentEnabled(entity, false);
            }

            if (GridSystem.IsWalkable(pathRequest.targetPosition, gridWidth, gridHeight, gridCellSize, pathingCostMap))
            {
                // Walkable ask for navigation.
                // Unit mover will check if it's unreachable.
                var flowFieldRequest = flowFieldRequestComponentLookup[entity];
                flowFieldRequest.targetPosition = pathRequest.targetPosition;
                flowFieldRequestComponentLookup[entity] = flowFieldRequest;

                flowFieldRequestComponentLookup.SetComponentEnabled(entity, true);
            }
            else
            {
                // Unwalkable position, simply don't navigate.
                unitMover.targetPosition = localTransform.Position;
                flowFieldRequestComponentLookup.SetComponentEnabled(entity, false);
                flowFieldFollowerComponentLookup.SetComponentEnabled(entity, false);
            }
            /// [Deprecated]: Unreachable path calculation. Rather than doing all this complex calculations,
            /// units just check wether the current cell has been written to or not.
            /* if (FlowFieldExists(unitMover.ValueRW.targetPosition, gridData, out FlowField flowField))
            {
                if (!IsPathable(pathRequest.ValueRW.targetPosition, flowField, gridData, ref state))
                {
                    Debug.LogError("Pathfinding: UNREACHABLE");
                    unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;
                    flowFieldRequestEnabled.ValueRW = false;
                    flowFieldFollowerEnabled.ValueRW = false;
                }
            } */
        }

        pathRequestComponentLookup.SetComponentEnabled(entity, false);
    }
}

[BurstCompile]
[WithAll(typeof(FlowFieldFollower))]
public partial struct CheckStraightPathJob : IJobEntity
{

    [NativeDisableParallelForRestriction] public ComponentLookup<FlowFieldFollower> flowFieldFollowerComponentLookup;

    /// <summary>Used for physics queries.</summary>
    [ReadOnly] public CollisionWorld collisionWorld;
    public void Execute(in LocalTransform localTransform, ref UnitMover unitMover, Entity entity)
    {
        // Lookup local fetch for readability.
        FlowFieldFollower flowFieldFollower = flowFieldFollowerComponentLookup[entity];

        RaycastInput raycastInput = new RaycastInput
        {
            Start = localTransform.Position,
            End = flowFieldFollower.targetPosition,
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
            unitMover.targetPosition = flowFieldFollower.targetPosition;
            flowFieldFollowerComponentLookup.SetComponentEnabled(entity, false);
        }
    }
}

/// <summary>
/// Moves a unit towards its target position and adjusts the rotation to match the movement direction.
/// </summary>
[BurstCompile]
[WithAll(typeof(FlowFieldFollower))]
public partial struct FollowFlowFieldJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public ComponentLookup<FlowFieldFollower> flowFieldFollowerComponentLookup;
    [ReadOnly] public ComponentLookup<GridCell> gridCellComponentLookup;
    [ReadOnly] public NativeArray<Entity> globalGridCellIndexedArray;

    /// <summary><see cref="GridData"/> decomposed data to avoid nested collection usage.</summary>
    [ReadOnly] public int gridWidth;

    /// <summary><see cref="GridData"/> decomposed data to avoid nested collection usage.</summary>
    [ReadOnly] public int gridHeight;

    /// <summary><see cref="GridData"/> decomposed data to avoid nested collection usage.</summary>
    [ReadOnly] public float gridCellSize;

    public void Execute(ref LocalTransform localTransform, ref UnitMover unitMover, Entity entity)
    {
        FlowFieldFollower flowFieldFollower = flowFieldFollowerComponentLookup[entity];

        // Retrieve current grid cell's pathing vector and convert it to world space
        int2 coords = GridSystem.WorldPositionToCoords(localTransform.Position, gridCellSize);
        int globalCellIndex = GridSystem.GetGlobalIndex(coords, flowFieldFollower.flowFieldIndex, gridWidth, gridHeight);
        Entity currentCell = globalGridCellIndexedArray[globalCellIndex];
        GridCell gridCell = gridCellComponentLookup[currentCell];
        float3 worldMovementVector = GridSystem.GridVectorToWorldSpace(gridCell.pathingVector);

        // If inside a wall, use the previous cell's vector. Else, read cell vector.
        if (GridSystem.IsObstructed(gridCell))
        {
            worldMovementVector = flowFieldFollowerComponentLookup[entity].lastMoveVector;
        }
        else
        {
            flowFieldFollower.lastMoveVector = worldMovementVector;
        }

        // No path was found, stop movement.
        if (!GridSystem.IsPathable(gridCell) &&
            !GridSystem.IsObstructed(gridCell))
        {
            flowFieldFollowerComponentLookup.SetComponentEnabled(entity, false);
            unitMover.targetPosition = localTransform.Position;
            return;
        }

        unitMover.targetPosition =
            GridSystem.CoordsToWorldPositionCenter(coords, gridCellSize) +
            worldMovementVector * gridCellSize * 2;

        // Detect if the unit has reached its destination.
        if (math.distance(localTransform.Position, flowFieldFollower.targetPosition) < gridCellSize * 1.5f)
        {
            Debug.Log("Stopped unit");
            unitMover.targetPosition = localTransform.Position;
            flowFieldFollowerComponentLookup.SetComponentEnabled(entity,false);
        }

        // Overrite original lookup values.
        flowFieldFollowerComponentLookup[entity] = flowFieldFollower;
    }
}


/// <summary>
/// Moves a unit towards its target position and adjusts the rotation to match the movement direction.
/// </summary>
[BurstCompile]
public partial struct MoveUnitJob : IJobEntity
{
    //Set on struct construction
    [ReadOnly] public float deltaTime;

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

