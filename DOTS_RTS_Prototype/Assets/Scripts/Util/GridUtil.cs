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
    /// /// <remarks>
    /// Decoupled call parameters override for job access since <see cref="GridData"/> is unavailable due to nested native collections.
    /// </remarks>
    public static bool ValidateCoords(int2 coords, int gridSize)
    {
        return
            coords.x >= 0 &&
            coords.y >= 0 &&
            coords.x < gridSize &&
            coords.y < gridSize;
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
        Entity entity = GetCurrentCellEntity(currentPosition, flowfield, gridData);

        return IsPathable(state.EntityManager.GetComponentData<GridCell>(entity));
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
    int occluderSideSize,
    in GridData gridData,
    out int2 result)
    {
        int width = gridData.width;
        int height = gridData.height;

        if (!ValidateCoords(origin, width, height))
        {
            result = origin;
            return false;
        }

        // Expand square by one layer
        int expandedSize = occluderSideSize + 2;

        // Convert square size to extent from center
        int extent = expandedSize / 2;

        int minX = origin.x - extent;
        int maxX = origin.x + extent;

        int minY = origin.y - extent;
        int maxY = origin.y + extent;

        // TOP + BOTTOM
        for (int x = minX; x <= maxX; x++)
        {
            int2 top = new int2(x, maxY);

            if (ValidateCoords(top, width, height) &&
                !IsObstructed(top, gridData))
            {
                result = top;
                return true;
            }

            int2 bottom = new int2(x, minY);

            if (ValidateCoords(bottom, width, height) &&
                !IsObstructed(bottom, gridData))
            {
                result = bottom;
                return true;
            }
        }

        // LEFT + RIGHT
        for (int y = minY + 1; y < maxY; y++)
        {
            int2 left = new int2(minX, y);

            if (ValidateCoords(left, width, height) &&
                !IsObstructed(left, gridData))
            {
                result = left;
                return true;
            }

            int2 right = new int2(maxX, y);

            if (ValidateCoords(right, width, height) &&
                !IsObstructed(right, gridData))
            {
                result = right;
                return true;
            }
        }

        result = origin;
        return false;
    }
}