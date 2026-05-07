using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static GridSystem;

public static class GridUtil
{
    /// <summary>
    /// Calculates the movement vector from one 2D position to another.
    /// </summary>
    public static float2 CalculateFlowVector(int fromX, int fromY, int toX, int toY)
    {
        return new float2(toX, toY) - new float2(fromX, fromY);
    }

    /// <summary>
    /// Calculates the movement vector from one 2D position to another.
    /// </summary>
    public static float2 CalculateFlowVector(int2 fromPosition, int2 toPosition)
    {
        return new float2(toPosition.x - fromPosition.x, toPosition.y - fromPosition.y);
    }

    /// <summary>
    /// Converts a world space length into chunk grid space.
    /// </summary>a
    /// <remarks> 
    /// Used for calculating the original chunk grid size.
    /// </remarks>
    public static int GridChunkDims(int worldSpaceSize)
    {
        return (int)Mathf.Ceil((float)worldSpaceSize / (float)CHUNK_MAX_SIZE);
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
    /// Converts 2D grid coordinates to a flat array index.
    /// </summary>
    public static int ChunkCoordsToIndex(int x, int y, int width, int chunkSize)
    {
        return x + width / chunkSize * y;
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
    public static int GetGlobalCellIndex(int2 coords, int flowFieldIndex, int width, int height)
    {
        int totalCount = width * height;
        return totalCount * flowFieldIndex + CoordsToIndex(coords, width);
    }

    public static NativeList<int2> GetCoordFootprint(int2 origin, int gridWidth, int gridHeight, int2 size)
    {
        NativeList<int2> result = new NativeList<int2>(Allocator.Temp);

        for (int dx = 0; dx < size.x; dx++)
        {
            for (int dy = 0; dy < size.y; dy++)
            {
                int2 coord = new int2(origin.x + dx, origin.y + dy);

                if (ValidateCoords(coord, gridWidth, gridHeight))
                {
                    result.Add(coord);
                }
            }
        }

        return result;
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
    /// Calculates the world position of the given grid chunk's center point.
    /// </summary>
    public static float3 CoordsToWorldPositionCenter(int cx, int cy, float cellSize, int chunkSize)
    {
        float worldChunkSize = cellSize * chunkSize;
        return new float3(
            x: cx * worldChunkSize + worldChunkSize / 2,
            y: 0.1f,
            z: cy * worldChunkSize + worldChunkSize / 2);
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
    /// Converts a world-space position into a grid-snapped world position.
    /// </summary>
    public static float3 SnapWorldPosition(float3 worldPosition, float gridCellSize)
    {
        return GridUtil.CoordsToWorldPositionCenter(
                    GridUtil.WorldPositionToCoords(worldPosition, gridCellSize), gridCellSize);
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

    [BurstCompile]
    public static bool TryGetNearestNeighbouringCell(
    int2 origin,
    int maxRadius,
    in GridData gridData,
    out int2 result)
    {
        int width = gridData.width;
        int height = gridData.height;

        // Original cell valid
        if (!ValidateCoords(origin, width, height))
        {
            result = origin;
            return false;
        }
        if (ValidateCoords(origin, width, height) &&
            !IsObstructed(origin, gridData))
        {
            result = origin;
            return true;
        }

        // Expand outward ring-by-ring
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            int minX = origin.x - radius;
            int maxX = origin.x + radius;

            int minY = origin.y - radius;
            int maxY = origin.y + radius;

            // TOP + BOTTOM rows
            for (int x = minX; x <= maxX; x++)
            {
                // Top
                int2 top = new int2(x, maxY);

                if (ValidateCoords(top, width, height) &&
                    !IsObstructed(top, gridData))
                {
                    result = top;
                    return true;
                }

                // Bottom
                int2 bottom = new int2(x, minY);

                if (ValidateCoords(bottom, width, height) &&
                    !IsObstructed(bottom, gridData))
                {
                    result = bottom;
                    return true;
                }
            }

            // LEFT + RIGHT columns
            for (int y = minY + 1; y < maxY; y++)
            {
                // Left
                int2 left = new int2(minX, y);

                if (ValidateCoords(left, width, height) &&
                    !IsObstructed(left, gridData))
                {
                    result = left;
                    return true;
                }

                // Right
                int2 right = new int2(maxX, y);

                if (ValidateCoords(right, width, height) &&
                    !IsObstructed(right, gridData))
                {
                    result = right;
                    return true;
                }
            }
        }

        result = origin;
        return false;
    }

    [BurstCompile]
    public static bool TryGetNearestNeighbouringCell(
    int2 origin,
    int maxRadius,
    int gridWidth,
    int gridHeight,
    NativeArray<byte> pathingCostMap,
    out int2 result)
    {
        // Origin valid
        if (ValidateCoords(origin, gridWidth, gridHeight))
        {
            int originIndex = CoordsToIndex(origin, gridWidth);

            if (pathingCostMap[originIndex] != GridSystem.OBSTRUCTED_COST)
            {
                result = origin;
                return true;
            }
        }

        // Expanding square rings
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            int minX = origin.x - radius;
            int maxX = origin.x + radius;

            int minY = origin.y - radius;
            int maxY = origin.y + radius;

            // TOP + BOTTOM
            for (int x = minX; x <= maxX; x++)
            {
                // Top
                if (TryCell(x, maxY, gridWidth, gridHeight, pathingCostMap, out result))
                    return true;

                // Bottom
                if (TryCell(x, minY, gridWidth, gridHeight, pathingCostMap, out result))
                    return true;
            }

            // LEFT + RIGHT
            for (int y = minY + 1; y < maxY; y++)
            {
                // Left
                if (TryCell(minX, y, gridWidth, gridHeight, pathingCostMap, out result))
                    return true;

                // Right
                if (TryCell(maxX, y, gridWidth, gridHeight, pathingCostMap, out result))
                    return true;
            }
        }

        result = origin;
        return false;
    }

    [BurstCompile]
    private static bool TryCell(
        int x,
        int y,
        int gridWidth,
        int gridHeight,
        NativeArray<byte> pathingCostMap,
        out int2 result)
    {
        // Unsigned bounds check = fastest version
        if ((uint)x >= (uint)gridWidth ||
            (uint)y >= (uint)gridHeight)
        {
            result = default;
            return false;
        }

        int index = x + y * gridWidth;

        if (pathingCostMap[index] == GridSystem.OBSTRUCTED_COST)
        {
            result = default;
            return false;
        }

        result = new int2(x, y);
        return true;
    }
}