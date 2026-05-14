using System.Globalization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using static GridUtil;

/// <summary>
/// System responsible for scheduling and managing unit movement simulation jobs.
/// </summary>
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(BuildPhysicsWorld))]
partial struct UnitMoverSystem : ISystem
{
    /// <summary>
    /// Job handle array for parallel jobs. Allocated once and disposed on system destroy.
    /// </summary>
    private NativeArray<JobHandle> jobHandleArray;

    /// <summary>Component lookup for <see cref="PathRequest"/> components. Used to access and modify pathfinding requests on entities.</summary>
    public ComponentLookup<PathRequest> pathRequestComponentLookup;

    /// <summary>Component lookup for <see cref="FlowFieldRequest"/> components. Used to access and modify flow field navigation requests on entities.</summary>
    public ComponentLookup<FlowFieldRequest> flowFieldRequestComponentLookup;

    /// <summary>Component lookup for <see cref="FlowFieldFollower"/> components. Used to access and modify flow field navigation state on entities.</summary>
    public ComponentLookup<FlowFieldFollower> flowFieldFollowerComponentLookup;

    /// <summary>Component lookup for <see cref="ManualMove"/> components. Used to access and modify manual movement state on entities.</summary>
    public ComponentLookup<ManualMove> manualMoveComponentLookup;

    /// <summary>Component lookup for <see cref="Targetter"/> components. Used to access and read targets on entities.</summary>
    public ComponentLookup<Targetter> targetterComponentLookup;

    /// <summary>Component lookup for <see cref="Targetter"/> components. Used to read building entities.</summary>
    public ComponentLookup<Building> buildingComponentLookup;

    /// <summary>Component lookup for <see cref="LocalTransform"/> components. Used to access building positions.</summary>
    public ComponentLookup<LocalTransform> localTransformComponentLookup;

    /// <summary>Component lookup for <see cref="GridCell"/> components. Used to access grid cell data for navigation and pathfinding.</summary>
    public ComponentLookup<GridCell> gridCellComponentLookup;

    /// <summary>
    /// Called when the system is created. Registers required singletons and initializes component lookups and persistent arrays.
    /// </summary>
    /// <param name="state">The system state for initialization.</param>
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
        targetterComponentLookup = SystemAPI.GetComponentLookup<Targetter>(isReadOnly: true);
        buildingComponentLookup = SystemAPI.GetComponentLookup<Building>(isReadOnly: true);
        localTransformComponentLookup = SystemAPI.GetComponentLookup<LocalTransform>(isReadOnly: true);
        gridCellComponentLookup = SystemAPI.GetComponentLookup<GridCell>(isReadOnly: false);
    }

    /// <summary>
    /// Schedules all movement jobs for units: initializes target positions, handles pathfinding, checks for direct paths, follows flow fields, and applies movement and rotation.
    /// </summary>
    /// <param name="state">The system state for update.</param>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency.Complete();
        // Get data for jobs.
        GridData gridData = SystemAPI.GetSingleton<GridData>();
        CollisionWorld collisionWorld = state.EntityManager.GetCollisionWorld();

        // Update lookups to get latest frame data.
        pathRequestComponentLookup.Update(ref state);
        flowFieldFollowerComponentLookup.Update(ref state);
        flowFieldRequestComponentLookup.Update(ref state);
        manualMoveComponentLookup.Update(ref state);
        targetterComponentLookup.Update(ref state);
        buildingComponentLookup.Update(ref state);
        localTransformComponentLookup.Update(ref state);
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
            collisionWorld = state.EntityManager.GetCollisionWorld(),
            gridWidth = gridData.width,
            gridHeight = gridData.height,
            gridCellSize = gridData.gridCellSize
        };
        pathRequestJob.ScheduleParallel();

        // Check if a straight path to target is available to skip navigation.
        CheckStraightPathJob checkStraightPathJob = new CheckStraightPathJob
        {
            flowFieldFollowerComponentLookup = flowFieldFollowerComponentLookup,
            targetterComponentLookup = targetterComponentLookup,
            buildingComponentLookup = buildingComponentLookup,
            localTransformComponentLookup = localTransformComponentLookup,
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
            manualMoveComponentLookup = manualMoveComponentLookup,
            deltaTime = SystemAPI.Time.DeltaTime
        };
        moveUnitJob.ScheduleParallel();
    }

    /// <summary>
    /// Called when the system is destroyed. Disposes persistent native arrays to prevent memory leaks.
    /// </summary>
    /// <param name="state">The system state for destruction.</param>
    [BurstCompile]
    private void OnDestroy(ref SystemState state)
    {
        jobHandleArray.Dispose();
    }
}

/// <summary>
/// Job that ensures a unit's target position is initialized after spawning. Prevents units from moving to (0,0,0) by default.
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
            Debug.Log("Initialized position.");
            unitMover.targetPosition = localTransform.Position;
        }
    }
}

/// <summary>
/// Job that manages unit pathfinding requests. Handles raycast checks for direct paths and requests flow field navigation if needed.
/// </summary>
[BurstCompile]
[WithAll(typeof(PathRequest))]
public partial struct PathRequestJob : IJobEntity
{

    /// <summary>Component lookup for <see cref="PathRequest"/> components. Used to access pathfinding requests on entities.</summary>
    [NativeDisableParallelForRestriction] public ComponentLookup<PathRequest> pathRequestComponentLookup;

    /// <summary>Component lookup for <see cref="FlowFieldFollower"/> components. Used to enable/disable flow field navigation on entities.</summary>
    [NativeDisableParallelForRestriction] public ComponentLookup<FlowFieldFollower> flowFieldFollowerComponentLookup;

    /// <summary>Component lookup for <see cref="FlowFieldRequest"/> components. Used to request flow field navigation for entities.</summary>
    [NativeDisableParallelForRestriction] public ComponentLookup<FlowFieldRequest> flowFieldRequestComponentLookup;

    /// <summary>Component lookup for <see cref="ManualMove"/> components. Used to disable manual movement when pathfinding is required.</summary>
    [NativeDisableParallelForRestriction] public ComponentLookup<ManualMove> manualMoveComponentLookup;

    /// <summary>Cell pathing cost map from <see cref="GridData"/>. Used to check walkability of target positions.</summary>
    [ReadOnly] public NativeArray<byte> pathingCostMap;

    /// <summary>Collision world for physics raycasts to check for obstructions.</summary>
    [ReadOnly] public CollisionWorld collisionWorld;

    /// <summary>Grid width from <see cref="GridData"/> (number of cells in X direction).</summary>
    [ReadOnly] public int gridWidth;
    /// <summary>Grid height from <see cref="GridData"/> (number of cells in Y direction).</summary>
    [ReadOnly] public int gridHeight;
    /// <summary>Grid cell size from <see cref="GridData"/> (world units per cell).</summary>
    [ReadOnly] public float gridCellSize;


    public void Execute(
        in LocalTransform localTransform,
        ref UnitMover unitMover,
        Entity entity)
    {
        // Lookup local fetch for readability.
        PathRequest pathRequest = pathRequestComponentLookup[entity];
        /* Debug.Log($"[PathingRequest] {entity.Index} Resolving PATH REQUEST to {pathRequest.targetPosition} - {pathRequest.postFormationPosition}"); */

        // If no formation required, set formation position to anchor position.
        if (pathRequest.postFormationPosition.Equals(float3.zero))
        {
            pathRequest.postFormationPosition = pathRequest.targetPosition;
        }

        // Check if valid position
        if (!ValidateCoords(
                WorldPositionToCoords(pathRequest.targetPosition, gridCellSize),
                gridWidth))
        {
            Debug.Log("INVALID COORDS");
            pathRequest.targetPosition = localTransform.Position;
            pathRequest.postFormationPosition = localTransform.Position;
            unitMover.targetPosition = localTransform.Position;

            if (manualMoveComponentLookup.HasComponent(entity))
            {
                manualMoveComponentLookup.SetComponentEnabled(entity, false);
            }

            pathRequestComponentLookup.SetComponentEnabled(entity, false);
            return;
        }

        // Check if a straight path to target is available. If not, request navigation.
        RaycastInput targetRaycastInput = new RaycastInput
        {
            Start = localTransform.Position,
            End = pathRequest.targetPosition,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.OBSTRUCTION_LAYER | 1u << GameAssets.BUILDINGS_LAYER,
                GroupIndex = 0
            }
        };

        RaycastInput formationRaycastInput = new RaycastInput
        {
            Start = localTransform.Position,
            End = pathRequest.postFormationPosition,
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.OBSTRUCTION_LAYER | 1u << GameAssets.BUILDINGS_LAYER,
                GroupIndex = 0
            }
        };

        if (!collisionWorld.CastRay(formationRaycastInput))
        {
            unitMover.targetPosition = pathRequest.postFormationPosition;
            flowFieldRequestComponentLookup.SetComponentEnabled(entity, false);
            flowFieldFollowerComponentLookup.SetComponentEnabled(entity, false);
            /* Debug.Log($"{entity.Index} Going for STRAIGHT FORMATION: {unitMover.targetPosition}"); */
        }
        else if (!collisionWorld.CastRay(targetRaycastInput))
        {
            // Hit nothing: moving straight towards target.
            unitMover.targetPosition = pathRequest.targetPosition;
            flowFieldRequestComponentLookup.SetComponentEnabled(entity, false);
            flowFieldFollowerComponentLookup.SetComponentEnabled(entity, false);
            /* Debug.Log($"{entity.Index} Going for STRAIGHT TARGET: {unitMover.targetPosition}"); */
        }
        else
        {
            // Obstructed path, might require navigation.

            // FIX This causes a bug where currently attacking units cannot be moved
            if (manualMoveComponentLookup.HasComponent(entity))
            {
                manualMoveComponentLookup.SetComponentEnabled(entity, false);
                /* Debug.Log("Going for FLOWFIELD NAVIGATION. Disabling MANUAL MOVE"); */
            }

            if (IsWalkable(pathRequest.targetPosition, gridWidth, gridHeight, gridCellSize, pathingCostMap))
            {
                Debug.Log("Navigation to walkable. FollowFlowFieldJob should check if unreachable.");
                // Walkable: ask for navigation.
                // Unit mover will check if it's unreachable.
                var flowFieldRequest = flowFieldRequestComponentLookup[entity];
                flowFieldRequest.targetPosition = pathRequest.targetPosition;
                flowFieldRequest.postFormationPosition = pathRequest.postFormationPosition;
                flowFieldRequestComponentLookup[entity] = flowFieldRequest;

                flowFieldRequestComponentLookup.SetComponentEnabled(entity, true);
            }
            else
            {
                Debug.Log($"Navigation UNREACHABLE. DON'T NAVIGATE. {pathRequest.postFormationPosition}");
                // Unwalkable position, simply don't navigate.
                unitMover.targetPosition = localTransform.Position;
                flowFieldRequestComponentLookup.SetComponentEnabled(entity, false);
                flowFieldFollowerComponentLookup.SetComponentEnabled(entity, false);
            }
            // [Deprecated]: Unreachable path calculation. Rather than doing all this complex calculation,
            // units just check whether the current cell has been written to or not.
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

/// <summary>
/// Job that checks if a straight path to the target is available for units with <see cref="FlowFieldFollower"/>. If so, disables navigation and moves directly.
/// </summary>
[BurstCompile]
[WithAll(typeof(FlowFieldFollower))]
public partial struct CheckStraightPathJob : IJobEntity
{
    /// <summary>Component lookup for <see cref="FlowFieldFollower"/> components. Used to enable/disable flow field navigation on entities.</summary>
    [NativeDisableParallelForRestriction] public ComponentLookup<FlowFieldFollower> flowFieldFollowerComponentLookup;

    /// <summary>Component lookup for <see cref="Targetter"/> components. Used to check if the target is a building.</summary>
    [ReadOnly] public ComponentLookup<Targetter> targetterComponentLookup;

    /// <summary>Component lookup for <see cref="Building"/> components. Used to check if the target is a building.</summary>
    [ReadOnly] public ComponentLookup<Building> buildingComponentLookup;

    /* /// <summary>Component lookup for <see cref="Unit"/> components. Used to check if the target is a unit.</summary>
    [ReadOnly] public ComponentLookup<Unit> unitComponentLookup; */

    /// <summary>Component lookup for <see cref="LocalTransform"/> components. Used to get building position.</summary>
    [ReadOnly] public ComponentLookup<LocalTransform> localTransformComponentLookup;

    /// <summary>Collision world for physics raycasts to check for obstructions.</summary>
    [ReadOnly] public CollisionWorld collisionWorld;
    public void Execute(ref UnitMover unitMover, Entity entity)
    {
        // Lookup local fetch for readability.
        FlowFieldFollower flowFieldFollower = flowFieldFollowerComponentLookup[entity];
        LocalTransform localTransform = localTransformComponentLookup[entity];

        // Target straight path rechecks
        if (targetterComponentLookup.HasComponent(entity))
        {
            var targetter = targetterComponentLookup[entity];

            if (buildingComponentLookup.HasComponent(targetter.targetEntity))
            {
                var buildingPosition = localTransformComponentLookup[targetter.targetEntity].Position;
                RaycastInput buildingRaycastInput = new RaycastInput
                {
                    Start = localTransform.Position,
                    End = buildingPosition,
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u,
                        CollidesWith = 1u << GameAssets.OBSTRUCTION_LAYER | 1u << GameAssets.BUILDINGS_LAYER,
                        GroupIndex = 0
                    }
                };

                if (collisionWorld.CastRay(buildingRaycastInput, out var hit))
                {
                    if (hit.Entity == targetter.targetEntity)
                    {
                        // Hit nothing. Take a straight path
                        unitMover.targetPosition = buildingPosition;
                        flowFieldFollowerComponentLookup.SetComponentEnabled(entity, false);
                        Debug.Log($"Recheck STRAIGHT BUILDING. {unitMover.targetPosition}");
                    }
                }
            }

            return;
        }

        // Target position straight path recheck
        {
            RaycastInput formationRaycastInput = new RaycastInput
            {
                Start = localTransform.Position,
                End = flowFieldFollower.postFormationPosition,
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.OBSTRUCTION_LAYER | 1u << GameAssets.BUILDINGS_LAYER,
                    GroupIndex = 0
                }
            };

            if (!collisionWorld.CastRay(formationRaycastInput))
            {
                // Hit nothing. Take a straight path
                unitMover.targetPosition = flowFieldFollower.postFormationPosition;
                flowFieldFollowerComponentLookup.SetComponentEnabled(entity, false);
                Debug.Log("Recheck STRAIGHT FORMATION");
            }
        }


        /* else if (!collisionWorld.CastRay(raycastInput))
        {
            // Hit nothing. Take a straight path
            unitMover.targetPosition = flowFieldFollower.targetPosition;
            flowFieldFollowerComponentLookup.SetComponentEnabled(entity, false);
            Debug.Log("Recheck STRAIGHT TARGET");
        } */
    }
}

/// <summary>
/// Job that moves a unit towards its target position using flow field navigation. Updates the target position and disables navigation if the destination is reached or blocked.
/// </summary>
[BurstCompile]
[WithAll(typeof(FlowFieldFollower))]
public partial struct FollowFlowFieldJob : IJobEntity
{

    /// <summary>Component lookup for <see cref="FlowFieldFollower"/> components. Used to access and update navigation state.</summary>
    [NativeDisableParallelForRestriction] public ComponentLookup<FlowFieldFollower> flowFieldFollowerComponentLookup;
    /// <summary>Component lookup for <see cref="GridCell"/> components. Used to read grid cell navigation data.</summary>
    [ReadOnly] public ComponentLookup<GridCell> gridCellComponentLookup;
    /// <summary>Array of all grid cell entities indexed globally for fast lookup.</summary>
    [ReadOnly] public NativeArray<Entity> globalGridCellIndexedArray;

    /// <summary>Grid width from <see cref="GridData"/> (number of cells in X direction).</summary>
    [ReadOnly] public int gridWidth;
    /// <summary>Grid height from <see cref="GridData"/> (number of cells in Y direction).</summary>
    [ReadOnly] public int gridHeight;
    /// <summary>Grid cell size from <see cref="GridData"/> (world units per cell).</summary>
    [ReadOnly] public float gridCellSize;

    public void Execute(ref LocalTransform localTransform, ref UnitMover unitMover, Entity entity)
    {
        FlowFieldFollower flowFieldFollower = flowFieldFollowerComponentLookup[entity];

        // Retrieve current grid cell's pathing vector and convert it to world space
        int2 coords = WorldPositionToCoords(localTransform.Position, gridCellSize);
        int globalCellIndex = GetGlobalCellIndex(coords, flowFieldFollower.flowFieldIndex, gridWidth, gridHeight);
        Entity currentCell = globalGridCellIndexedArray[globalCellIndex];
        GridCell gridCell = gridCellComponentLookup[currentCell];
        float3 worldMovementVector = GridVectorToWorldSpace(gridCell.flowVector);

        /* Debug.Log($"Following FLOWFIELD {entity.Index} {worldMovementVector}"); */

        // If inside a wall, use the previous cell's vector. Else, read cell vector.
        if (IsObstructed(gridCell))
        {
            worldMovementVector = flowFieldFollowerComponentLookup[entity].lastMoveVector;
        }
        else
        {
            flowFieldFollower.lastMoveVector = worldMovementVector;
        }

        // No path was found, stop movement.
        if (!IsPathable(gridCell) &&
            !IsObstructed(gridCell))
        {
            flowFieldFollowerComponentLookup.SetComponentEnabled(entity, false);
            unitMover.targetPosition = localTransform.Position;
            Debug.Log("Target UNREACHABLE. STOP.");
            return;
        }

        unitMover.targetPosition =
            CoordsToWorldPositionCenter(coords, gridCellSize) +
            worldMovementVector * gridCellSize * 2;

        // Detect if the unit has reached its destination.
        if (math.distance(localTransform.Position, flowFieldFollower.targetPosition) < gridCellSize * 1.5f ||
            math.distance(localTransform.Position, flowFieldFollower.postFormationPosition) < gridCellSize * 1.5f)
        {
            Debug.Log("Target REACHED. STOP.");
            unitMover.targetPosition = localTransform.Position;
            flowFieldFollowerComponentLookup.SetComponentEnabled(entity, false);
        }

        // Overwrite original lookup values.
        flowFieldFollowerComponentLookup[entity] = flowFieldFollower;
    }
}


/// <summary>
/// Job that moves a unit towards its target position and rotates it to face the movement direction. Applies velocity and stops movement when the target is reached.
/// </summary>
[BurstCompile]
public partial struct MoveUnitJob : IJobEntity
{
    /// <summary>Component lookup for <see cref="ManualMove"/> components. Used to access and modify manual movement state on entities.</summary>
    [NativeDisableParallelForRestriction] public ComponentLookup<ManualMove> manualMoveComponentLookup;

    /// <summary>
    /// Delta time for movement calculations.
    /// </summary>
    [ReadOnly] public float deltaTime;

    public void Execute(
        ref LocalTransform localTransform,
        ref UnitMover unitMover,
        ref PhysicsVelocity physicsVelocity,
        Entity entity)
    {
        // Desired normalized move direction based on positional difference
        float3 moveDirection = unitMover.targetPosition - localTransform.Position;
        /* Debug.Log($"Move direction: {moveDirection}"); */


        float targetReachedDistanceSquared = unitMover.targetReachedDistanceSquared; 
        if (math.lengthsq(moveDirection) <= targetReachedDistanceSquared)
        {
            /* Debug.Log($"Target reached. STOP. {moveDirection}"); */
            // Reached target
            physicsVelocity.Linear = float3.zero;
            physicsVelocity.Angular = float3.zero;
            unitMover.isMoving = false;
            
            manualMoveComponentLookup.SetComponentEnabled(entity, false);
            return;
        }
        unitMover.isMoving = true;

        moveDirection = math.normalize(moveDirection);

        // Rotate unit towards move direction
        localTransform.Rotation =
            math.slerp(localTransform.Rotation, quaternion.LookRotation(moveDirection, math.up()), deltaTime * unitMover.rotationSpeed);

        // Apply linear velocity and clamp angular (safety measure for constraint failures)
        physicsVelocity.Linear = moveDirection * unitMover.moveSpeed;
        physicsVelocity.Angular = float3.zero;

        // Transform movement alternative:
        // localTransform.ValueRW.Position += moveDirection * unitMover.ValueRO.value * SystemAPI.Time.DeltaTime;
    }
}

