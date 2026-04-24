using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Manages the runtime grid instance and exposes cell interaction for debug visualization.
/// </summary>
/// <remarks>
/// Grid creation is deferred until the first update frame because the baked <see cref="GridDataParameters"/> singleton
/// may not be available yet during conversion and editor bake execution.
/// </remarks>
[BurstCompile]
partial struct GridSystem : ISystem
{
    public const int UNPATHABLE_COST = int.MaxValue;
    public const int OBSTRUCTED_COST = byte.MaxValue;
    public const int WEIGHTED_COST = 50;
    public const int FLOW_FIELD_MAP_COUNT = 100;
    public ComponentLookup<GridCell> gridCellComponentLookup;


    /// <summary>
    /// Executes grid display initialization once the grid data registry is available.
    /// </summary>
    /// <remarks>
    /// This runs after the first successful update since it requires post-bake components, unavailabla before OnUpdate.
    /// Otherwise, this logic would run inside OnCreate.
    /// </remarks>
    [BurstCompile]
    private void OnLateCreate(ref SystemState state)
    {
        GridData gridData = SystemAPI.GetComponent<GridData>(state.SystemHandle);

        InitializeDebugVisual(gridData);
        UpdateDebugVisual(gridData);
    }

    [BurstDiscard]
    private static void InitializeDebugVisual(GridData gridData)
    {
        GridDebugDisplay.Instance?.InitializeGrid(gridData);
    }

    [BurstDiscard]
    private static void UpdateDebugVisual(GridData gridData)
    {
        GridDebugDisplay.Instance?.UpdateGridVisual(gridData);
    }

    [BurstDiscard]
    private static void UpdateDebugVisual(GridData gridData, int i)
    {
        GridDebugDisplay.Instance?.UpdateGridVisual(gridData, i);
    }


    /// <summary>
    /// Creates the runtime grid and stores the generated grid metadata on the system entity.
    /// </summary>
    /// <remarks>
    /// This runs after the first successful update since it requires the baked <see cref="GridDataParameters"/> singleton, unavailabla before OnUpdate.
    /// Otherwise, this logic would run inside OnCreate.
    /// </remarks>
    /// <returns>True if the grid was created this frame; otherwise false.</returns>
    [BurstCompile]
    private bool TryCreateGrid(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton(out GridDataParameters gridData))
        {
            return false;
        }

        // Get baked parameters and destroy its temporary container.
        Entity gridDataEntity = SystemAPI.GetSingletonEntity<GridDataParameters>();
        state.EntityManager.DestroyEntity(gridDataEntity);

        // Make a gridCell template for instantiation.
        int totalCellCount = gridData.width * gridData.height;
        Entity gridCellEntityTemplate = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<GridCell>(gridCellEntityTemplate);

        // TODO: Optimize
        NativeArray<FlowField> flowFieldArray =
            new NativeArray<FlowField>(FLOW_FIELD_MAP_COUNT, Allocator.Persistent);
        NativeList<Entity> globalGridCellList =
            new NativeList<Entity>(totalCellCount * FLOW_FIELD_MAP_COUNT, Allocator.Persistent);

        // Generate required empty flowfields.
        for (int i = 0; i < FLOW_FIELD_MAP_COUNT; i++)
        {
            FlowField flowField = new FlowField
            {
                gridCellEntityArray = new NativeArray<Entity>(totalCellCount, Allocator.Persistent)
            };
            flowField.isCalculated = false;

            // Generate required empty grid cells inside flowfields.
            state.EntityManager.Instantiate(gridCellEntityTemplate, flowField.gridCellEntityArray);
            globalGridCellList.AddRange(flowField.gridCellEntityArray);

            // Set base data for each cell inside each flowfield.
            for (int x = 0; x < gridData.width; x++)
            {
                for (int y = 0; y < gridData.height; y++)
                {
                    int index = CoordsToIndex(x, y, gridData.width);
                    GridCell gridCell = new GridCell
                    {
                        flowFieldIndex = i,
                        index = index,
                        x = x,
                        y = y,
                    };

                    Entity cellEntity = flowField.gridCellEntityArray[index];

                    state.EntityManager.SetName(cellEntity, $"GridCell-{x},{y}");
                    SystemAPI.SetComponent(cellEntity, gridCell);
                }
            }

            flowFieldArray[i] = flowField;
        }

        Debug.Log("World grid built successfully");

        state.EntityManager.AddComponent<GridData>(state.SystemHandle);
        state.EntityManager.SetComponentData(
            state.SystemHandle,
            new GridData
            {
                width = gridData.width,
                height = gridData.height,
                gridCellSize = gridData.gridCellSize,
                flowFieldArray = flowFieldArray,
                pathingCostMap = new NativeArray<byte>(totalCellCount, Allocator.Persistent),
                globalGridCellIndexedArray = globalGridCellList.ToArray(Allocator.Persistent)
            });

        globalGridCellList.Dispose();
        gridCellComponentLookup = SystemAPI.GetComponentLookup<GridCell>(isReadOnly: false);

        gridData.isInitialized = true;
        return true;
    }

    /// <summary>
    /// Creates the runtime grid when the baked <see cref="GridDataParameters"/> singleton appears,
    /// then handles per-frame grid interaction and debug updates.
    /// </summary>
    /* [BurstCompile] */
    public void OnUpdate(ref SystemState state)
    {
        if (TryCreateGrid(ref state))
        {
            OnLateCreate(ref state);
        }

        // Grid creation validation: deferred Update
        if (!SystemAPI.HasComponent<GridData>(state.SystemHandle))
        {
            return;
        }

        gridCellComponentLookup.Update(ref state);

        GridData gridData = SystemAPI.GetComponent<GridData>(state.SystemHandle);

        // ===============================================
        // PATHING START
        // ===============================================

        // Path requests
        foreach ((
            RefRW<FlowFieldRequest> flowFieldRequest,
            EnabledRefRW<FlowFieldRequest> flowFieldRequestEnabled,
            RefRW<FlowFieldFollower> flowFieldFollower,
            EnabledRefRW<FlowFieldFollower> flowFieldFollowerEnabled,
            RefRW<UnitMover> unitMover)
                in SystemAPI.Query<
                RefRW<FlowFieldRequest>,
                EnabledRefRW<FlowFieldRequest>,
                RefRW<FlowFieldFollower>,
                EnabledRefRW<FlowFieldFollower>,
                RefRW<UnitMover>>().
                WithPresent<FlowFieldFollower>())
        {

            int2 targetCoords = WorldPositionToCoords(flowFieldRequest.ValueRO.targetPosition, gridData.gridCellSize);

            // Resolving request.
            flowFieldRequestEnabled.ValueRW = false;

            // Fetch pre-existing matching FlowField if there is one.
            bool existingPath = false;
            for (int i = 0; i < FLOW_FIELD_MAP_COUNT; i++)
            {
                if (gridData.flowFieldArray[i].targetCoords.Equals(targetCoords))
                {
                    flowFieldFollower.ValueRW.flowFieldIndex = i;
                    flowFieldFollower.ValueRW.targetPosition = flowFieldRequest.ValueRO.targetPosition;
                    flowFieldFollowerEnabled.ValueRW = true;
                    UpdateDebugVisual(gridData, i);

                    existingPath = true;
                    break;
                }
            }
            if (existingPath)
            {
                continue;
            }

            int flowFieldIndex = gridData.nextFlowFieldIndex; // FIX Use LoopCounter
            gridData.nextFlowFieldIndex = (gridData.nextFlowFieldIndex + 1) % FLOW_FIELD_MAP_COUNT; // FIX Use LoopCounter

            // Proceed with pathfinding
            flowFieldFollower.ValueRW.flowFieldIndex = flowFieldIndex;
            flowFieldFollower.ValueRW.targetPosition = flowFieldRequest.ValueRO.targetPosition;
            flowFieldFollowerEnabled.ValueRW = true;

            NativeArray<RefRW<GridCell>> gridCellArray =
            new NativeArray<RefRW<GridCell>>(gridData.width * gridData.height, Allocator.Temp);

            // Set all pathing costs to default values.
            {
                InitializeGridJob initializeGridJob = new InitializeGridJob
                {
                    flowFieldIndex = flowFieldIndex,
                    targetCoords = targetCoords
                };
                JobHandle initializeGridJobHandle = initializeGridJob.ScheduleParallel(state.Dependency);
                initializeGridJobHandle.Complete();

                // Add all cells to indexes.
                for (int x = 0; x < gridData.width; x++)
                {
                    for (int y = 0; y < gridData.height; y++)
                    {
                        int index = CoordsToIndex(x, y, gridData.width);
                        Entity cellEntity = gridData.flowFieldArray[flowFieldIndex].gridCellEntityArray[index];
                        RefRW<GridCell> gridCell = SystemAPI.GetComponentRW<GridCell>(cellEntity);

                        gridCellArray[index] = gridCell;
                    }
                }
                UpdateDebugVisual(gridData);
            }

            // Obstructed cell detection and cost calculation
            {
                CollisionWorld collisionWorld = state.EntityManager.GetCollisionWorld();
                var obstructedCollisionFilter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.OBSTRUCTION_LAYER,
                    GroupIndex = 0
                };
                var weightedCollisionFilter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.WEIGHTED_LAYER,
                    GroupIndex = 0
                };

                UpdatePathingCostJob updatePathingCostJob = new UpdatePathingCostJob
                {
                    pathingCostMap = gridData.pathingCostMap,
                    flowField = gridData.flowFieldArray[flowFieldIndex],
                    collisionWorld = collisionWorld,
                    gridWidth = gridData.width,
                    gridCellSize = gridData.gridCellSize,
                    overlapSphereRadius = gridData.gridCellSize * .5f,
                    obstructedCollisionFilter = obstructedCollisionFilter,
                    weightedCollisionFilter = weightedCollisionFilter

                };

                JobHandle updatePathingCostJobHandle = updatePathingCostJob.ScheduleParallel(state.Dependency);
                updatePathingCostJobHandle.Complete();
            }


            // FlowField Calculation.
            {
                // BFS Queue, started on target
                NativeQueue<RefRW<GridCell>> gridCellOpenQueue = new NativeQueue<RefRW<GridCell>>(Allocator.Temp);
                RefRW<GridCell> targetGridCell = gridCellArray[CoordsToIndex(targetCoords, gridData.width)];
                gridCellOpenQueue.Enqueue(targetGridCell);

                //TODO: Document logic extensively
                // Process all cells in the queue using breadth-first search for uniform cost pathfinding.
                while (!gridCellOpenQueue.IsEmpty())
                {
                    // Retrieve the next cell from the open queue and find all cells adjacent to it.
                    RefRW<GridCell> currGridCell = gridCellOpenQueue.Dequeue();
                    using NativeList<RefRW<GridCell>> neighbouringCellsList =
                        GetNeighbouringCellsRecursive(currGridCell, gridData, gridCellArray);
                    foreach (RefRW<GridCell> neighbourCell in neighbouringCellsList)
                    {
                        // If obstructed cell, skip
                        if (neighbourCell.ValueRO.stepCost == OBSTRUCTED_COST)
                        {
                            continue;
                        }
                        // If a new best path is discovered through the cell, update it's data and recurse.
                        int newBestCost = (currGridCell.ValueRO.bestPathCost + neighbourCell.ValueRO.stepCost);
                        if (newBestCost < neighbourCell.ValueRO.bestPathCost)
                        {
                            // Update the cell's best known cost to reach the target and store the vector for path reconstruction.
                            neighbourCell.ValueRW.bestPathCost = newBestCost;
                            neighbourCell.ValueRW.pathingVector = CalculateVector(
                                fromPosition: IndexToCoords(neighbourCell.ValueRO.index, gridData.width),
                                toPosition: IndexToCoords(currGridCell.ValueRO.index, gridData.width)
                            );

                            gridCellOpenQueue.Enqueue(neighbourCell);
                        }
                    }
                }
                gridCellArray.Dispose();
                gridCellOpenQueue.Dispose();
            }

            // Set all data values
            {
                // Set data values for calculated flowfield.
                FlowField flowField = gridData.flowFieldArray[flowFieldIndex];
                flowField.targetCoords = targetCoords;
                flowField.isCalculated = true;
                gridData.flowFieldArray[flowFieldIndex] = flowField;

                // Set component data
                SystemAPI.SetComponent<GridData>(state.SystemHandle, gridData);
                // Show debug visuals.
                UpdateDebugVisual(gridData);
            }
        }
    }

    /// <summary>
    /// Releases persistent grid resources when the system is destroyed.
    /// </summary>
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        RefRW<GridData> gridData = SystemAPI.GetComponentRW<GridData>(state.SystemHandle);

        for (int i = 0; i < FLOW_FIELD_MAP_COUNT; i++)
        {
            gridData.ValueRW.flowFieldArray[i].gridCellEntityArray.Dispose();
        }
        gridData.ValueRW.flowFieldArray.Dispose();
        gridData.ValueRW.pathingCostMap.Dispose();
        gridData.ValueRW.globalGridCellIndexedArray.Dispose();
    }

    public static NativeList<RefRW<GridCell>> GetNeighbouringCells(
        RefRW<GridCell> gridCell,
        GridData gridData,
        NativeArray<RefRW<GridCell>> gridCellArray)
    {
        return GetNeighbouringCells(gridCell, gridData, gridCellArray, radius: 1);
    }

    public static NativeList<RefRW<GridCell>> GetNeighbouringCells(
        RefRW<GridCell> gridCell,
        GridData gridData,
        NativeArray<RefRW<GridCell>> gridCellArray,
        int radius)
    {
        NativeList<RefRW<GridCell>> neighbourList = new NativeList<RefRW<GridCell>>(Allocator.Temp);

        int x0 = gridCell.ValueRO.x;
        int y0 = gridCell.ValueRO.y;

        for (int x = 0 - radius; x <= radius; x++)
        {
            for (int y = 0 - radius; y <= radius; y++)
            {
                // If original cell, skip.
                if (x == 0 && y == 0) continue;

                // Get position, if out of bounds skip.
                int2 coords = new int2(x0 + x, y0 + y);
                if (!ValidateCoords(coords, gridData)) continue;

                // Get grid cell RefRW.
                int index = CoordsToIndex(coords, gridData.width);
                neighbourList.Add(gridCellArray[index]);
            }
        }

        return neighbourList;
    }

    public static NativeList<RefRW<GridCell>> GetNeighbouringCellsRecursive(
        RefRW<GridCell> gridCell,
        GridData gridData,
        NativeArray<RefRW<GridCell>> gridCellArray)
    {
        return GetNeighbouringCellsRecursive(gridCell, gridData, gridCellArray, radius: 1);
    }

    public static NativeList<RefRW<GridCell>> GetNeighbouringCellsRecursive(
    RefRW<GridCell> startCell,
    GridData gridData,
    NativeArray<RefRW<GridCell>> gridCellArray,
    int radius)
    {
        NativeList<RefRW<GridCell>> result = new NativeList<RefRW<GridCell>>(Allocator.Temp);

        int2 startPos = new int2(startCell.ValueRO.x, startCell.ValueRO.y);

        //IDEA: Extracting might imrpove performance
        // Direction priority
        NativeArray<int2> directions = new NativeArray<int2>(8, Allocator.Temp);
        {
            directions[0] = new int2(0, 1);   // Up
            directions[1] = new int2(1, 0);   // Right
            directions[2] = new int2(0, -1);  // Down
            directions[3] = new int2(-1, 0);  // Left
            directions[4] = new int2(1, 1);   // TopRight
            directions[5] = new int2(1, -1);  // BotRight
            directions[6] = new int2(-1, -1); // BotLeft
            directions[7] = new int2(-1, 1);  // TopLeft
        }

        // Track visited to avoid duplicates
        NativeHashSet<int> visited = new NativeHashSet<int>(gridData.width * gridData.height, Allocator.Temp);

        void Expand(int2 currentPos, int currentRadius)
        {
            if (currentRadius > radius) return;

            foreach (int2 dir in directions)
            {
                int2 nextPos = currentPos + dir;

                if (!ValidateCoords(nextPos, gridData)) continue;

                // Skip if already visited
                int index = CoordsToIndex(nextPos, gridData.width);
                if (!visited.Add(index)) continue;

                RefRW<GridCell> cell = gridCellArray[index];
                result.Add(cell);

                // Recurse outward
                Expand(nextPos, currentRadius + 1);
            }
        }

        // Start recursion
        Expand(startPos, 1);

        visited.Dispose();
        directions.Dispose();

        return result;
    }

    /// <summary>
    /// Calculates the movement vector from one 2D position to another.
    /// </summary>
    public static float2 CalculateVector(int fromX, int fromY, int toX, int toY)
    {
        return new float2(toX, toY) - new float2(fromX, fromY);
    }

    /// <summary>
    /// Calculates the movement vector from one 2D position to another.
    /// </summary>
    public static float2 CalculateVector(int2 fromPosition, int2 toPosition)
    {
        return new float2(toPosition.x - fromPosition.x, toPosition.y - fromPosition.y);
    }

    /// <summary>
    /// Converts 2D grid coordinates to a flat array index.
    /// </summary>
    public static int CoordsToIndex(int x, int y, int width)
    {
        return x + width * y;
    }

    /// <summary>
    /// Converts 2D grid coordinates to a flat array index.
    /// </summary>
    public static int CoordsToIndex(int2 coords, int width)
    {
        return coords.x + width * coords.y;
    }

    /// <summary>
    /// Converts a flat array index to 2D grid coordinates.
    /// </summary>
    public static int2 IndexToCoords(int index, int width)
    {
        int x = index % width;
        int y = index / width;
        return new int2(x, y);
    }

    /// <summary>
    /// Converts 2D parameters in a specific <see cref="FlowField"/> into a global index. Used for accesing <see cref="GridData.globalGridCellIndexedArray"/> since nested native collections are unavailable inside jobs.
    /// </summary>
    public static int GetGlobalIndex(int2 coords, int flowFieldIndex, int width, int height)
    {
        int totalCount = width * height;
        return totalCount * flowFieldIndex + CoordsToIndex(coords, width);
    }

    /// <summary>
    /// Calculates the world position of the given grid cell's origin corner.
    /// </summary>
    public static float3 CoordsToWorldPositionCorner(int x, int y, float cellSize)
    {
        return new float3(
            x: x * cellSize,
            y: 0.1f,
            z: y * cellSize);
    }

    /// <summary>
    /// Calculates the world position of the given grid cell's center point.
    /// </summary>
    public static float3 CoordsToWorldPositionCenter(int x, int y, float cellSize)
    {
        return new float3(
            x: x * cellSize + cellSize / 2,
            y: 0.1f,
            z: y * cellSize + cellSize / 2);
    }

    /// <summary>
    /// Calculates the world position of the given grid cell's center point.
    /// </summary>
    public static float3 CoordsToWorldPositionCenter(int2 coords, float cellSize)
    {
        return new float3(
            x: coords.x * cellSize + cellSize / 2,
            y: 0.1f,
            z: coords.y * cellSize + cellSize / 2);
    }

    /// <summary>
    /// Converts a world-space position into a grid coordinate.
    /// </summary>
    public static int2 WorldPositionToCoords(float3 worldPosition, float gridCellSize)
    {
        return new int2(
            (int)math.floor(worldPosition.x / gridCellSize),
            (int)math.floor(worldPosition.z / gridCellSize)
        );
    }

    /// <summary>
    /// Returns true when the supplied grid coordinates are inside the grid bounds.
    /// </summary>
    public static bool ValidateCoords(int2 coords, GridData gridData)
    {
        return
            coords.x >= 0 &&
            coords.y >= 0 &&
            coords.x < gridData.width &&
            coords.y < gridData.height;
    }

    /// <summary>
    /// Returns true when the supplied grid coordinates are inside the grid bounds.
    /// </summary>
    /// /// <remarks>
    /// Decoupled call parameters override for job access since <see cref="GridData"/> is unavailable due to nested native collections.
    /// </remarks>
    public static bool ValidateCoords(int2 coords, int gridWidth, int gridHeight)
    {
        return
            coords.x >= 0 &&
            coords.y >= 0 &&
            coords.x < gridWidth &&
            coords.y < gridHeight;
    }

    /// <summary>
    /// Returns true when the supplied grid coordinates are inside the grid bounds.
    /// </summary>
    public static float3 GridVectorToWorldSpace(float2 vector)
    {
        return new float3(vector.x, 0, vector.y);
    }

    /// <summary>
    /// Returns true when the supplied grid cell represents an obstructed cell.
    /// </summary>
    public static bool IsObstructed(GridCell cell)
    {
        return cell.stepCost == OBSTRUCTED_COST;
    }

    /// <summary>
    /// Returns true when the grid cell in the coordinates represents an obstructed cell.
    /// </summary>
    public static bool IsObstructed(int2 coords, GridData gridData)
    {
        int index = CoordsToIndex(coords, gridData.width);
        return gridData.pathingCostMap[index] == OBSTRUCTED_COST;
    }

    /// <summary>
    /// Returns true when the grid cell in the coordinates represents an obstructed cell.
    /// </summary>
    /// <remarks>
    /// Decoupled call parameters override for job access since <see cref="GridData"/> is unavailable due to nested native collections.
    /// </remarks>
    public static bool IsObstructed(int2 coords, int gridWidth, NativeArray<byte> pathingCostMap)
    {
        int index = CoordsToIndex(coords, gridWidth);
        return pathingCostMap[index] == OBSTRUCTED_COST;
    }

    /// <summary>
    /// Returns true when the supplied grid cell represents a walkable cell.
    /// </summary>
    public static bool IsWalkable(float3 worldPosition, GridData gridData)
    {
        int2 coords = WorldPositionToCoords(worldPosition, gridData.gridCellSize);
        return ValidateCoords(coords, gridData) && !IsObstructed(coords, gridData);
    }

    /// <summary>
    /// Returns true when the supplied grid cell represents a walkable cell.
    /// </summary>
    /// /// <remarks>
    /// Decoupled call parameters override for job access since <see cref="GridData"/> is unavailable due to nested native collections.
    /// </remarks>
    public static bool IsWalkable(float3 worldPosition, int gridWidth, int gridHeight, float gridCellSize, NativeArray<byte> pathingCostMap)
    {
        int2 coords = WorldPositionToCoords(worldPosition, gridCellSize);
        return ValidateCoords(coords, gridWidth, gridHeight) && !IsObstructed(coords, gridWidth, pathingCostMap);
    }

    /// <summary>
    /// Returns true when the supplied grid cell can reach its target destination.
    /// </summary>
    public static bool IsPathable(GridCell cell)
    {
        return cell.bestPathCost < UNPATHABLE_COST;
    }

    /// <summary>
    /// Returns true when the supplied grid cell represents an obstructed cell.
    /// </summary>
    public static bool IsPathable(float3 currentPosition, FlowField flowfield, GridData gridData, ref SystemState state)
    {
        Entity e = GetCurrentCellEntity(currentPosition, flowfield, gridData);

        return IsPathable(state.EntityManager.GetComponentData<GridCell>(e));
    }

    public static Entity GetCurrentCellEntity(float3 currentPosition, FlowField flowfield, GridData gridData)
    {
        int2 coords = WorldPositionToCoords(currentPosition, gridData.gridCellSize);
        int cellIndex = CoordsToIndex(coords, gridData.width);

        return flowfield.gridCellEntityArray[cellIndex];
    }

    public static bool FlowFieldExists(int2 targetCoords, GridData gridData, out FlowField flowField)
    {
        for (int i = 0; i < FLOW_FIELD_MAP_COUNT; i++)
        {
            if (gridData.flowFieldArray[i].targetCoords.Equals(targetCoords))
            {
                flowField = gridData.flowFieldArray[i];
                return true;
            }
        }
        flowField = default;
        return false;
    }

    public static bool FlowFieldExists(float3 targetPosition, GridData gridData, out FlowField flowField)
    {
        int2 targetCoords = WorldPositionToCoords(targetPosition, gridData.gridCellSize);
        return FlowFieldExists(targetCoords, gridData, out flowField);
    }


    /* for (int i = 0; i<FLOW_FIELD_MAP_COUNT; i++)
            {
                if (gridData.flowFieldArray[i].targetCoords.Equals(targetCoords))
                {
                    flowFieldFollower.ValueRW.flowFieldIndex = i;
                    flowFieldFollower.ValueRW.targetPosition = flowFieldRequest.ValueRO.targetPosition;
                    flowFieldFollowerEnabled.ValueRW = true;

                    GridDebugDisplay.Instance?.UpdateGridVisual(gridData, i);

    existingPath = true;
                    break;
                }
            } */

}

[BurstCompile]
public partial struct InitializeGridJob : IJobEntity
{
    [ReadOnly] public int flowFieldIndex;
    [ReadOnly] public int2 targetCoords;

    public void Execute(ref GridCell gridCell)
    {
        // If the cell is not inside the required FlowField, skip
        if (gridCell.flowFieldIndex != flowFieldIndex)
        {
            return;
        }

        /* gridCellArray[index] = gridCell; */

        gridCell.pathingVector = new Vector2(0, 1); // Safety measure for in-clipping spawns.
        if (gridCell.x == targetCoords.x &&
            gridCell.y == targetCoords.y)
        {
            // Cell is the target destination.
            gridCell.stepCost = 0;
            gridCell.bestPathCost = 0;
        }
        else
        {
            gridCell.stepCost = 1;
            gridCell.bestPathCost = int.MaxValue;
        }
    }
}

[BurstCompile]
public partial struct UpdatePathingCostJob : IJobEntity
{
    /// <summary>Cell pathing cost map inside <see cref="GridData"/>.</summary>
    [NativeDisableParallelForRestriction] public NativeArray<byte> pathingCostMap;

    /// <summary>Used to identify which flowfield is being updated.</summary>
    [ReadOnly] public FlowField flowField;

    /// <summary>Used for physics queries.</summary>
    [ReadOnly] public CollisionWorld collisionWorld;

    /// <summary><see cref="GridData"/> decomposed data to avoid nested collection usage.</summary>
    [ReadOnly] public int gridWidth;

    /// <summary><see cref="GridData"/> decomposed data to avoid nested collection usage.</summary>
    [ReadOnly] public float gridCellSize;

    /// <summary>Physics query decomposed parameters, cached for optimization.</summary>
    [ReadOnly] public float overlapSphereRadius;

    /// <summary>Physics query decomposed parameters, cached for optimization.</summary>
    [ReadOnly] public CollisionFilter obstructedCollisionFilter;

    /// <summary>Physics query decomposed parameters, cached for optimization.</summary>
    [ReadOnly] public CollisionFilter weightedCollisionFilter;

    public void Execute(ref GridCell gridCell)
    {
        NativeList<DistanceHit> distanceHitList = new NativeList<DistanceHit>(Allocator.TempJob);

        // If detecting an obstructed cell, set its cost.
        if (collisionWorld.OverlapSphere(
                position: GridSystem.CoordsToWorldPositionCenter(gridCell.x, gridCell.y, gridCellSize),
                radius: overlapSphereRadius,
                ref distanceHitList,
                obstructedCollisionFilter
            ))
        {
            gridCell.stepCost = GridSystem.OBSTRUCTED_COST;
            int index = GridSystem.CoordsToIndex(gridCell.x, gridCell.y, gridWidth);
            pathingCostMap[index] = GridSystem.OBSTRUCTED_COST;
        }

        // If detecting a weighted cell, set its cost.
        if (collisionWorld.OverlapSphere(
                position: GridSystem.CoordsToWorldPositionCenter(gridCell.x, gridCell.y, gridCellSize),
                radius: overlapSphereRadius,
                ref distanceHitList,
                weightedCollisionFilter
            ))
        {
            gridCell.stepCost = GridSystem.WEIGHTED_COST;
            int index = GridSystem.CoordsToIndex(gridCell.x, gridCell.y, gridWidth);
            pathingCostMap[index] = GridSystem.WEIGHTED_COST;
        }

        distanceHitList.Dispose();
    }
}

/// <summary>
/// Stores baked grid configuration and the runtime grid entity map for the system.
/// </summary>
public struct GridData : IComponentData
{
    /// <summary>Grid width in cells.</summary>
    public int width;

    /// <summary>Grid height in cells.</summary>
    public int height;

    /// <summary>Size of a single grid cell in world units.</summary>
    public float gridCellSize;

    /// <summary>Entity lookup map for every created grid cell.</summary>
    public NativeArray<FlowField> flowFieldArray;

    /// <summary>Next index to fill in <see cref="flowFieldArray"/> when calculating a new FlowField.</summary>
    public int nextFlowFieldIndex;

    /// <summary>Map for every grid's flow field cost.</summary>
    public NativeArray<byte> pathingCostMap;

    /// <summary>Index for every existing grid cell from every single <see cref="FlowField"/>.</summary>
    public NativeArray<Entity> globalGridCellIndexedArray;

}

/// <summary>
/// Holds the runtime entity mapping for spawned grid cells.
/// </summary>
public struct FlowField : IComponentData
{
    /// <summary>Flat entity array containing every grid cell.</summary>
    public NativeArray<Entity> gridCellEntityArray;

    /// <summary>Target coordinates towards which the flow field is calculated.</summary>
    public int2 targetCoords;

    /// <summary>Wether the flow field has been calculated.</summary>
    public bool isCalculated;
}

/// <summary>
/// Represents a single logical cell inside the runtime grid.
/// </summary>
public struct GridCell : IComponentData
{
    /// <summary>Flow field index that identifies which <see cref="FlowField"/> the cell belongs to.</summary>
    public int flowFieldIndex;

    /// <summary>Cell unique index for collection identification.</summary>
    public int index;

    /// <summary>Grid X coordinate.</summary>
    public int x;

    /// <summary>Grid Y coordinate.</summary>
    public int y;

    /// <summary>Movement or cost value used by pathing.</summary>
    public byte stepCost;

    /// <summary>Cached best cost for pathing calculations.</summary>
    public int bestPathCost;

    /// <summary>Direction vector to the next cell on a path.</summary>
    public float2 pathingVector;
}

/// <summary>
/// Represents a single logical cell inside the runtime grid.
/// </summary>
public struct GridChunk : IComponentData
{
    
    /// <summary>Flow field index that identifies which <see cref="FlowField"/> the cell belongs to.</summary>
    public int flowFieldIndex;

    /// <summary>Cell unique index for collection identification.</summary>
    public int index;
    public int2 originCoords;

    /// <summary>Dimension size of chunk (64x64 Chunk => 64 squareSize).</summary>
    public int squareSize;

    public bool occluded;
    public bool viewed;

    /// <summary>Cached best cost for pathing calculations.</summary>
    public int bestPathCost;

    /// <summary>Direction vector to the next cell on a path.</summary>
    public float2 pathingVector;
}
