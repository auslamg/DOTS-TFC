using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Manages the runtime grid instance and exposes cell interaction for debug visualization.
/// </summary>
/// <remarks>
/// Grid creation is deferred until the first update frame because the baked <see cref="GridData"/> singleton
/// may not be available yet during conversion and editor bake execution.
/// </remarks>
partial struct GridSystem : ISystem
{
    private int2 targetGridPosition;
    /// <summary>
    /// Executes grid display initialization once the grid data registry is available.
    /// </summary>
    private void OnLateCreate(ref SystemState state)
    {
        GridDataRegistry gridDataRegistry = SystemAPI.GetComponent<GridDataRegistry>(state.SystemHandle);

        GridDebugDisplay.Instance?.InitializeGrid(gridDataRegistry);
        GridDebugDisplay.Instance?.UpdateGridVisual(gridDataRegistry);
    }

    /// <summary>
    /// Creates the runtime grid and stores the generated grid metadata on the system entity.
    /// </summary>
    /// <returns>True if the grid was created this frame; otherwise false.</returns>
    private bool TryCreateGrid(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton(out GridData gridData))
        {
            return false;
        }

        Entity gridDataEntity = SystemAPI.GetSingletonEntity<GridData>();
        state.EntityManager.DestroyEntity(gridDataEntity);

        int totalCount = gridData.width * gridData.height;
        Entity gridCellEntityTemplate = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<GridCell>(gridCellEntityTemplate);

        GridMap gridMap = new GridMap
        {
            gridCellEntityArray = new NativeArray<Entity>(totalCount, Allocator.Persistent)
        };

        state.EntityManager.Instantiate(gridCellEntityTemplate, gridMap.gridCellEntityArray);

        Debug.Log("Building world grid");

        for (int x = 0; x < gridData.width; x++)
        {
            for (int y = 0; y < gridData.height; y++)
            {
                int index = CoordsToIndex(x, y, gridData.width);
                GridCell gridCell = new GridCell
                {
                    index = index,
                    x = x,
                    y = y,
                };

                Entity cellEntity = gridMap.gridCellEntityArray[index];

                state.EntityManager.SetName(cellEntity, $"GridCell({x},{y})");
                SystemAPI.SetComponent(cellEntity, gridCell);
            }
        }

        Debug.Log("World grid built successfully");

        state.EntityManager.AddComponent<GridDataRegistry>(state.SystemHandle);
        state.EntityManager.SetComponentData(
            state.SystemHandle,
            new GridDataRegistry
            {
                width = gridData.width,
                height = gridData.height,
                gridCellSize = gridData.gridCellSize,
                gridMap = gridMap
            });

        gridData.isInitialized = true;
        return true;
    }

    /// <summary>
    /// Creates the runtime grid when the baked <see cref="GridData"/> singleton appears,
    /// then handles per-frame grid interaction and debug updates.
    /// </summary>
    public void OnUpdate(ref SystemState state)
    {
        if (TryCreateGrid(ref state))
        {
            OnLateCreate(ref state);
        }

        if (!SystemAPI.HasComponent<GridDataRegistry>(state.SystemHandle))
        {
            return;
        }

        GridDataRegistry gridData = SystemAPI.GetComponent<GridDataRegistry>(state.SystemHandle);

        // ===============================================
        //PATHING START
        // ===============================================

        NativeArray<RefRW<GridCell>> gridCellArray =
            new NativeArray<RefRW<GridCell>>(gridData.width * gridData.height, Allocator.Temp);

        // Set all pathing costs to default values.
        for (int x = 0; x < gridData.width; x++)
        {
            for (int y = 0; y < gridData.height; y++)
            {
                int index = CoordsToIndex(x, y, gridData.width);
                Entity cellEntity = gridData.gridMap.gridCellEntityArray[index];
                RefRW<GridCell> gridCell = SystemAPI.GetComponentRW<GridCell>(cellEntity);

                gridCellArray[index] = gridCell;

                gridCell.ValueRW.pathingVector = new Vector2(0, 1); // Safety measure for in-clipping spawns.
                if (x == targetGridPosition.x &&
                    y == targetGridPosition.y)
                {
                    // Cell is the target destination.
                    gridCell.ValueRW.stepCost = 0;
                    gridCell.ValueRW.bestPathCost = 0;
                }
                else
                {
                    gridCell.ValueRW.stepCost = 1;
                    gridCell.ValueRW.bestPathCost = byte.MaxValue;
                }
            }
        }

        using (NativeQueue<RefRW<GridCell>> gridCellOpenQueue = new NativeQueue<RefRW<GridCell>>(Allocator.Temp))
        {
            RefRW<GridCell> targetGridCell = gridCellArray[CoordsToIndex(targetGridPosition, gridData.width)];
            gridCellOpenQueue.Enqueue(targetGridCell);

            // Process all cells in the queue using breadth-first search for uniform cost pathfinding.
            while (!gridCellOpenQueue.IsEmpty())
            {
                // Retrieve the next cell from the open queue and find all cells adjacent to it.
                RefRW<GridCell> currGridCell = gridCellOpenQueue.Dequeue();
                using NativeList<RefRW<GridCell>> neighbouringCellsList = 
                    GetNeighbouringCellsRecursive(currGridCell, gridData, gridCellArray);
                foreach (RefRW<GridCell> neighbourCell in neighbouringCellsList)
                {
                    // If a new best path is discovered through the cell, update it's data and recurse.
                    byte newBestCost = (byte)(currGridCell.ValueRO.bestPathCost + neighbourCell.ValueRO.stepCost);
                    if (newBestCost < neighbourCell.ValueRO.bestPathCost)
                    {
                        // Update the neighbor's best known cost to reach the target and store the vector for path reconstruction.
                        neighbourCell.ValueRW.bestPathCost = newBestCost;
                        neighbourCell.ValueRW.pathingVector = CalculateVector(
                            fromPosition: IndexToCoords(neighbourCell.ValueRO.index, gridData.width),
                            toPosition: IndexToCoords(currGridCell.ValueRO.index, gridData.width)
                        );

                        gridCellOpenQueue.Enqueue(neighbourCell);
                    }
                }
            }
            /* Debug.Log($"FLOWFIELD: Checked {10000-safety} cells"); */

            gridCellArray.Dispose();
        }

        GridDebugDisplay.Instance?.UpdateGridVisual(gridData);

        // TEST: Used for testing grid cell interaction.
        if (Input.GetMouseButtonDown(0))
        {
            float3 mouseWorldPosition = MouseWorldPosition.Instance.GetPosition();
            int2 mouseGridPosition = CalculateGridPosition(mouseWorldPosition, gridData.gridCellSize);

            if (ValidateGridPosition(mouseGridPosition, gridData))
            {
                int index = CoordsToIndex(mouseGridPosition.x, mouseGridPosition.y, gridData.width);
                Entity gridCellEntity = gridData.gridMap.gridCellEntityArray[index];
                RefRW<GridCell> gridCell = SystemAPI.GetComponentRW<GridCell>(gridCellEntity);
                /* gridCell.ValueRW.data = 1; */
                Debug.Log($"Selected vector: {gridCell.ValueRO.pathingVector}");
                targetGridPosition = mouseGridPosition;

                GridDebugDisplay.Instance?.UpdateCellVisual(gridCell.ValueRO);
            }
        }
    }

    /// <summary>
    /// Releases persistent grid resources when the system is destroyed.
    /// </summary>
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        RefRW<GridDataRegistry> gridSystemData = SystemAPI.GetComponentRW<GridDataRegistry>(state.SystemHandle);
        gridSystemData.ValueRW.gridMap.gridCellEntityArray.Dispose();
    }

    public static NativeList<RefRW<GridCell>> GetNeighbouringCells(
        RefRW<GridCell> gridCell,
        GridDataRegistry gridData,
        NativeArray<RefRW<GridCell>> gridCellArray)
    {
        return GetNeighbouringCells(gridCell, gridData, gridCellArray, radius: 1);
    }

    public static NativeList<RefRW<GridCell>> GetNeighbouringCells(
        RefRW<GridCell> gridCell,
        GridDataRegistry gridData,
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
                int2 gridPosition = new int2(x0 + x, y0 + y);
                if (!ValidateGridPosition(gridPosition, gridData)) continue;

                // Get grid cell RefRW.
                int index = CoordsToIndex(gridPosition, gridData.width);
                neighbourList.Add(gridCellArray[index]);
            }
        }

        return neighbourList;
    }

    public static NativeList<RefRW<GridCell>> GetNeighbouringCellsRecursive(
        RefRW<GridCell> gridCell,
        GridDataRegistry gridData,
        NativeArray<RefRW<GridCell>> gridCellArray)
    {
        return GetNeighbouringCellsRecursive(gridCell, gridData, gridCellArray, radius: 1);
    }

    public static NativeList<RefRW<GridCell>> GetNeighbouringCellsRecursive(
    RefRW<GridCell> startCell,
    GridDataRegistry gridData,
    NativeArray<RefRW<GridCell>> gridCellArray,
    int radius)
    {
        NativeList<RefRW<GridCell>> result = new NativeList<RefRW<GridCell>>(Allocator.Temp);

        int2 startPos = new int2(startCell.ValueRO.x, startCell.ValueRO.y);

        // Direction priority
        int2[] directions = new int2[]
        {
        new int2(0, 1),   // Up
        new int2(1, 0),   // Right
        new int2(0, -1),  // Down
        new int2(-1, 0),  // Left
        new int2(1, 1),   // TopRight
        new int2(1, -1),  // BotRight
        new int2(-1, -1), // BotLeft
        new int2(-1, 1),  // TopLeft
        };

        // Track visited to avoid duplicates
        NativeHashSet<int> visited = new NativeHashSet<int>(gridData.width * gridData.height, Allocator.Temp);

        void Expand(int2 currentPos, int currentRadius)
        {
            if (currentRadius > radius) return;

            foreach (int2 dir in directions)
            {
                int2 nextPos = currentPos + dir;

                if (!ValidateGridPosition(nextPos, gridData)) continue;

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
    public static int CoordsToIndex(int2 gridPosition, int width)
    {
        return gridPosition.x + width * gridPosition.y;
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
    /// Calculates the world position of the given grid cell.
    /// </summary>
    public static Vector3 CalculateWorldPosition(int x, int y, float cellSize)
    {
        return new Vector3(x * cellSize, 0.1f, y * cellSize);
    }

    /// <summary>
    /// Converts a world-space position into a grid coordinate.
    /// </summary>
    public static int2 CalculateGridPosition(float3 worldPosition, float gridCellSize)
    {
        return new int2(
            (int)math.floor(worldPosition.x / gridCellSize),
            (int)math.floor(worldPosition.z / gridCellSize)
        );
    }

    /// <summary>
    /// Returns true when the supplied grid coordinates are inside the grid bounds.
    /// </summary>
    public static bool ValidateGridPosition(int2 gridPosition, GridDataRegistry gridData)
    {
        return
            gridPosition.x >= 0 &&
            gridPosition.y >= 0 &&
            gridPosition.x < gridData.width &&
            gridPosition.y < gridData.height;
    }
}

/// <summary>
/// Stores baked grid configuration and the runtime grid entity map for the system.
/// </summary>
public struct GridDataRegistry : IComponentData
{
    /// <summary>Grid width in cells.</summary>
    public int width;

    /// <summary>Grid height in cells.</summary>
    public int height;

    /// <summary>Size of a single grid cell in world units.</summary>
    public float gridCellSize;

    /// <summary>Entity lookup map for every created grid cell.</summary>
    public GridMap gridMap;
}

/// <summary>
/// Holds the runtime entity mapping for spawned grid cells.
/// </summary>
public struct GridMap : IComponentData
{
    /// <summary>Flat entity array containing every grid cell.</summary>
    public NativeArray<Entity> gridCellEntityArray;
}

/// <summary>
/// Represents a single logical cell inside the runtime grid.
/// </summary>
public struct GridCell : IComponentData
{
    /// <summary>Cell unique index for collection identification.</summary>
    public int index;
    /// <summary>Grid X coordinate.</summary>
    public int x;

    /// <summary>Grid Y coordinate.</summary>
    public int y;

    /// <summary>Movement or cost value used by pathing.</summary>
    public byte stepCost;

    /// <summary>Cached best cost for pathing calculations.</summary>
    public byte bestPathCost;

    /// <summary>Direction vector to the next cell on a path.</summary>
    public float2 pathingVector;
}
