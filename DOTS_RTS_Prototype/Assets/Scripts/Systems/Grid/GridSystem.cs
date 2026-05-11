using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static GridUtil;

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
    public const int CHUNK_MAX_SIZE = 8;
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
        InitializeMinimapCamera(gridData);
        UpdateManagedData(gridData);
    }

    [BurstDiscard]
    private void UpdateManagedData(GridData gridData)
    {
        BuildingPlacementManager.Instance?.SetGridData(gridData);
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

    [BurstDiscard]
    private static void InitializeMinimapCamera(GridData gridData)
    {
        MinimapCameraHandler.Instance?.InitializeCamera(gridData);
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
        if (!SystemAPI.TryGetSingleton(out GridDataParameters gridParams))
        {
            return false;
        }

        // Get baked grid parameters and destroy its temporary container.
        Entity gridDataEntity = SystemAPI.GetSingletonEntity<GridDataParameters>();
        state.EntityManager.DestroyEntity(gridDataEntity);

        // Generate flow fields.
        GenerateFlowFields(ref state, gridParams,
            out int totalCellCount,
            out NativeArray<FlowField> flowFieldArray,
            out NativeList<Entity> globalGridCellList);

        // Generate chunk grid.
        GenerateGridChunks(ref state, gridParams,
            out NativeArray<GridChunk> chunkArray);

        // Create component with generated data.
        state.EntityManager.AddComponent<GridData>(state.SystemHandle);
        state.EntityManager.SetComponentData(
            state.SystemHandle,
            new GridData
            {
                width = gridParams.width,
                height = gridParams.height,
                gridCellSize = gridParams.gridCellSize,
                flowFieldArray = flowFieldArray,
                pathingCostMap = new NativeArray<byte>(totalCellCount, Allocator.Persistent),
                pathingMapVersion = 0,
                chunkArray = chunkArray,
                globalGridCellIndexedArray = globalGridCellList.ToArray(Allocator.Persistent)
            });

        globalGridCellList.Dispose();
        gridCellComponentLookup = SystemAPI.GetComponentLookup<GridCell>(isReadOnly: false);

        gridParams.isInitialized = true;
        return true;
    }

    [BurstCompile]
    private void GenerateFlowFields(
        ref SystemState state,
        GridDataParameters gridParams,
        out int totalCellCount,
        out NativeArray<FlowField> flowFieldArray,
        out NativeList<Entity> globalGridCellEntityList)
    {
        // Make a grid cell template for instantiation.
        Entity gridCellEntityTemplate = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<GridCell>(gridCellEntityTemplate);

        // Calculate total cell count for allocation size.
        totalCellCount = gridParams.width * gridParams.height;

        // Allocate collections.
        flowFieldArray = new NativeArray<FlowField>(FLOW_FIELD_MAP_COUNT, Allocator.Persistent);
        globalGridCellEntityList = new NativeList<Entity>(totalCellCount * FLOW_FIELD_MAP_COUNT, Allocator.Persistent);

        // Generate required empty flowfields.
        {
            for (int i = 0; i < FLOW_FIELD_MAP_COUNT; i++)
            {
                FlowField flowField = new FlowField
                {
                    gridCellEntityArray = new NativeArray<Entity>(totalCellCount, Allocator.Persistent)
                };
                flowField.isCalculated = false;

                // Generate required empty grid cell entities inside flowfields.
                state.EntityManager.Instantiate(gridCellEntityTemplate, flowField.gridCellEntityArray);
                globalGridCellEntityList.AddRange(flowField.gridCellEntityArray);

                // Set base data for each cell inside each flowfield.
                for (int x = 0; x < gridParams.width; x++)
                {
                    for (int y = 0; y < gridParams.height; y++)
                    {
                        int index = CoordsToIndex(x, y, gridParams.width);
                        GridCell gridCell = new GridCell
                        {
                            flowFieldIndex = i,
                            index = index,
                            x = x,
                            y = y,
                            bestPathCost = byte.MaxValue
                        };

                        Entity cellEntity = flowField.gridCellEntityArray[index];

                        state.EntityManager.SetName(cellEntity, $"GridCell-{x},{y}");
                        SystemAPI.SetComponent(cellEntity, gridCell);
                    }
                }

                flowFieldArray[i] = flowField;
            }
        }

        Debug.Log("World flow grid built successfully");
    }

    [BurstCompile]
    private void GenerateGridChunks(
        ref SystemState state,
        GridDataParameters gridParams,
        out NativeArray<GridChunk> chunkArray)
    {
        // Create grid chunk template for instantiation.
        Entity gridChunkEntityTemplate = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<GridChunk>(gridChunkEntityTemplate);

        // Calculate total grid chunk count for allocation size.
        int gridChunkColumns = GridChunkDims(gridParams.width);
        int gridChunkRows = GridChunkDims(gridParams.height);
        int totalChunkCount = gridChunkColumns * gridChunkRows;

        // Allocate collections.
        var chunkEntityArray = new NativeArray<Entity>(totalChunkCount, Allocator.Persistent);
        chunkArray = new NativeArray<GridChunk>(totalChunkCount, Allocator.Persistent);

        // Generate initial empty chunks.
        state.EntityManager.Instantiate(gridChunkEntityTemplate, chunkEntityArray);

        // Set base data for each grid chunk.
        {
            for (int x = 0; x < gridChunkColumns; x++)
            {
                for (int y = 0; y < gridChunkRows; y++)
                {
                    int index = ChunkCoordsToIndex(x, y, gridParams.width, CHUNK_MAX_SIZE);
                    GridChunk gridChunk = new GridChunk
                    {
                        index = index,
                        cx = x,
                        cy = y,
                        visited = false
                    };

                    Entity chunkEntity = chunkEntityArray[index];
                    state.EntityManager.SetName(chunkEntity, $"GridChunk-{CHUNK_MAX_SIZE}x-{x},{y}");
                    SystemAPI.SetComponent(chunkEntity, gridChunk);

                    chunkArray[index] = gridChunk;
                }
            }
        }

        Debug.Log("World chunk grid built successfully");
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
        
        CalculateOcclusion(ref state, ref gridData);
        ResolvePathRequests(ref state, ref gridData);
    }

    [BurstCompile]
    private void CalculateOcclusion(ref SystemState state, ref GridData gridData)
    {
        foreach ((
                   RefRW<Occluder> occluder,
                   RefRO<LocalTransform> localTransform)
                       in SystemAPI.Query<
                       RefRW<Occluder>,
                       RefRO<LocalTransform>>())
        {
            // No new buildings - no need to update the occlusion.
            if (occluder.ValueRO.isAccountedFor) continue;
            Debug.Log("[OCCLUSION UPDATED]");

            float3 origin = localTransform.ValueRO.Position;
            origin.x -= (occluder.ValueRO.occlusionFootprint.x * gridData.gridCellSize / 2) - 1;
            origin.z -= (occluder.ValueRO.occlusionFootprint.y * gridData.gridCellSize / 2) - 1;

            int2 originCoords = GridUtil.WorldPositionToCoords(origin, gridData.gridCellSize);
            int2 occlusionFootprint = occluder.ValueRO.occlusionFootprint;

            NativeList<int2> obstructedCoords = GetCoordFootprint(originCoords, gridData.width, gridData.height, occlusionFootprint);

            /* Debug.Log($"[Occluder]: Loading occlusion at {localTransform.ValueRO.Position}"); */

            foreach (var coord in obstructedCoords)
            {
                int index = GridUtil.CoordsToIndex(coord, gridData.width);
                gridData.pathingCostMap[index] =
                    (byte)(occluder.ValueRO.markedForDeletion ? 0 : OBSTRUCTED_COST);

                /* Debug.Log($"[Occluder]: Occupying {coord}"); */
            }

            obstructedCoords.Dispose();
            occluder.ValueRW.isAccountedFor = true;

            gridData.UpdatePathingMapVersion();
            SystemAPI.SetComponent<GridData>(state.SystemHandle, gridData);

            UpdateDebugVisual(gridData);

            /* Debug.Log($"[Occluder]: Loaded occlusion at {localTransform.ValueRO.Position}"); */
        }
    }

    /* [BurstCompile] */
    private SystemState ResolvePathRequests(ref SystemState state, ref GridData gridData)
    {
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
            /* Debug.Log($"Resolving PATH request to {targetCoords}"); */

            // Resolving request.
            flowFieldRequestEnabled.ValueRW = false;

            // Fetch pre-existing matching FlowField if there is one.
            bool existingPath = false;
            for (int i = 0; i < FLOW_FIELD_MAP_COUNT; i++)
            {
                if (gridData.flowFieldArray[i].targetCoords.Equals(targetCoords) &&
                    gridData.pathingMapVersion == gridData.flowFieldArray[i].pathingMapVersion)
                {
                    flowFieldFollower.ValueRW.flowFieldIndex = i;
                    flowFieldFollower.ValueRW.targetPosition = flowFieldRequest.ValueRO.targetPosition;
                    flowFieldFollower.ValueRW.postFormationPosition = flowFieldRequest.ValueRO.postFormationPosition;
                    flowFieldFollowerEnabled.ValueRW = true;

                    UpdateDebugVisual(gridData, i);

                    existingPath = true;
                    break;
                }
            }
            if (existingPath)
            {
                /* Debug.Log($"FLOWFIELD FOUND: Index {flowFieldFollower.ValueRW.flowFieldIndex}. Exiting navigation."); */
                continue;
            }

            int flowFieldIndex = gridData.nextFlowFieldIndex;
            gridData.nextFlowFieldIndex = (gridData.nextFlowFieldIndex + 1) % FLOW_FIELD_MAP_COUNT;

            // Proceed with pathfinding
            flowFieldFollower.ValueRW.flowFieldIndex = flowFieldIndex;
            flowFieldFollower.ValueRW.targetPosition = flowFieldRequest.ValueRO.targetPosition;
            flowFieldFollower.ValueRW.postFormationPosition = flowFieldRequest.ValueRO.postFormationPosition;
            flowFieldFollowerEnabled.ValueRW = true;

            NativeArray<RefRW<GridCell>> gridCellArray =
                new NativeArray<RefRW<GridCell>>(gridData.width * gridData.height, Allocator.Temp);

            // Set all pathing costs to default values.
            {
                InitializeFlowFieldJob initializeGridJob = new InitializeFlowFieldJob
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

            // Account for occluders

            /* Debug.Log($"[Occluder]: Loading occlusion at {localTransform.ValueRO.Position}"); */
            // Apply pathingCostMap to current flowfield
            for (int i = 0; i < gridData.pathingCostMap.Length; i++)
            {
                if (gridData.pathingCostMap[i] != OBSTRUCTED_COST)
                    continue;

                RefRW<GridCell> cell = gridCellArray[i];

                cell.ValueRW.stepCost = OBSTRUCTED_COST;
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
                            neighbourCell.ValueRW.flowVector = CalculateFlowVector(
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
                Debug.Log($"[FlowField] Allocated flowfield {flowFieldIndex}");

                // Set data values for calculated flowfield.
                FlowField flowField = gridData.flowFieldArray[flowFieldIndex];
                flowField.targetCoords = targetCoords;
                flowField.isCalculated = true;
                flowField.pathingMapVersion = gridData.pathingMapVersion;
                gridData.flowFieldArray[flowFieldIndex] = flowField;

                // Set component data
                SystemAPI.SetComponent<GridData>(state.SystemHandle, gridData);
                // Show debug visuals.
                UpdateDebugVisual(gridData);
            }
        }

        return state;
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
        gridData.ValueRW.chunkArray.Dispose();
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

}

[BurstCompile]
public partial struct InitializeFlowFieldJob : IJobEntity
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

        gridCell.flowVector = new Vector2(0, 1); // Safety measure for in-clipping spawns.
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

    /// <summary>Pathing cost map versioning id to avoid reusing old flowfields without accounting for new buildings.</summary>
    public uint pathingMapVersion;

    /// <summary>Entity lookup map for every created grid cell.</summary>
    public NativeArray<GridChunk> chunkArray;

    /// <summary>Index for every existing grid cell from every single <see cref="FlowField"/>.</summary>
    public NativeArray<Entity> globalGridCellIndexedArray;

    public void UpdatePathingMapVersion()
    {
        Debug.Log("Updated Map Version");
        if (pathingMapVersion == uint.MaxValue)
            pathingMapVersion = 0;
        else
            pathingMapVersion += 1;
    }
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
    /// <summary>Pathing cost map versioning id to avoid reusing old flowfields without accounting for new buildings.</summary>
    public uint pathingMapVersion;
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
    public float2 flowVector;
}

/// <summary>
/// Represents a single logical cell inside the runtime grid.
/// </summary>
public struct GridChunk : IComponentData
{
    /// <summary>Cell unique index for collection identification.</summary>
    public int index;

    /// <summary>Chunk grid X coordinate.</summary>
    public int cx;

    /// <summary>Chunk grid Y coordinate.</summary>
    public int cy;

    /// <summary>Dimension size of chunk (64x64 Chunk => 64 squareSize).</summary>
    public int size;

    public bool obstructed;
    public bool visited;
}