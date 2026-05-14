using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using static GridSystem;
using static GridUtil;

/// <summary>
/// Manages runtime debug visualization of grid cells, chunks, and borders using ECS grid data.
/// </summary>
public class GridGrizmoDisplay : MonoBehaviour
{
    /// <summary>
    /// Prefab used to render individual grid cells.
    /// </summary>
    [Header("Cell gizmos")]
    [SerializeField] private Transform gridCellGizmo;

    /// <summary>
    /// Enables cell rendering.
    /// </summary>
    public bool showCells = true;

    /// <summary>
    /// Prefab used to render grid chunks.
    /// </summary>
    [Header("Cell gizmos")]
    [SerializeField] private Transform gridChunkGizmo;

    /// <summary>
    /// Enables chunk rendering.
    /// </summary>
    public bool showChunks = true;

    /// <summary>
    /// Prefab used for grid border visualization.
    /// </summary>
    [Header("Border gizmo")]
    [SerializeField] private Transform gridBorderGizmo;

    /// <summary>
    /// Enables border rendering.
    /// </summary>
    public bool showBorder = true;

    /// <summary>
    /// Sprite for default cell visualization.
    /// </summary>
    [Header("Sprites")]
    [SerializeField] private Sprite baseCell;

    /// <summary>
    /// Sprite used for directional flow cells.
    /// </summary>
    [SerializeField] private Sprite arrowCell;

    /// <summary>
    /// Sprite used for unreachable cells.
    /// </summary>
    [SerializeField] private Sprite noPathCell;

    /// <summary>
    /// Sprite used for chunk visualization.
    /// </summary>
    [SerializeField] private Sprite baseChunk;

    /// <summary>
    /// Indicates whether grid visualization has been initialized.
    /// </summary>
    private bool isInitialized = false;

    /// <summary>
    /// Cached grid cell gizmo instances.
    /// </summary>
    private GridCellGizmo[,] gridCellsArray;

    /// <summary>
    /// Cached grid chunk gizmo instances.
    /// </summary>
    private GridChunkGizmo[,] gridChunksArray;

    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static GridGrizmoDisplay Instance { get; private set; }

    private void InitializeSingleton()
    {
        if (Instance == null) Instance = this;
        else
        {
            Debug.LogError("Multiple instances of singleton found on " + gameObject.name);
            Destroy(this);
        }
    }

    private void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// Initializes full grid visualization.
    /// </summary>
    public void InitializeGrid(GridData gridData)
    {
        if (showCells)
        {
            gridCellsArray = new GridCellGizmo[gridData.width, gridData.height];

            for (int x = 0; x < gridData.width; x++)
                for (int y = 0; y < gridData.height; y++)
                {
                    Transform cellGizmo = Instantiate(gridCellGizmo, transform);
                    cellGizmo.name = $"CellGizmo-{x},{y}";

                    var cell = cellGizmo.GetComponent<GridCellGizmo>();
                    cell.Initialize(x, y, gridData.gridCellSize);

                    gridCellsArray[x, y] = cell;
                }
        }

        if (showChunks)
        {
            gridChunksArray = new GridChunkGizmo[
                gridData.width / CHUNK_MAX_SIZE,
                gridData.height / CHUNK_MAX_SIZE];

            for (int cx = 0; cx < gridData.width / CHUNK_MAX_SIZE; cx++)
                for (int cy = 0; cy < gridData.height / CHUNK_MAX_SIZE; cy++)
                {
                    Transform chunkGizmo = Instantiate(gridChunkGizmo, transform);
                    chunkGizmo.name = $"ChunkGizmo-{cx},{cy}";

                    var chunk = chunkGizmo.GetComponent<GridChunkGizmo>();
                    chunk.Initialize(cx, cy, gridData.gridCellSize, CHUNK_MAX_SIZE);

                    gridChunksArray[cx, cy] = chunk;
                }
        }

        if (showBorder)
        {
            float2 size = new float2(
                gridData.width * gridData.gridCellSize,
                gridData.height * gridData.gridCellSize);

            Transform border = Instantiate(gridBorderGizmo, transform);
            border.name = $"BorderGizmo-{gridData.width}x{gridData.height}";

            border.GetComponent<GridBorderGizmo>()
                  .Initialize(size.x, gridData.gridCellSize);
        }

        isInitialized = true;
    }

    /// <summary>
    /// Updates visualization using latest flow-field snapshot.
    /// </summary>
    public void UpdateGridVisual(GridData gridData)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        int index = Mathf.Max(gridData.nextFlowFieldIndex - 1, 0);

        for (int x = 0; x < gridData.width; x++)
            for (int y = 0; y < gridData.height; y++)
            {
                int cellIndex = CoordsToIndex(x, y, gridData.width);
                Entity e = gridData.flowFieldArray[index].gridCellEntityArray[cellIndex];
                var cell = entityManager.GetComponentData<GridCell>(e);

                UpdateCellVisual(cell);
            }

        for (int x = 0; x < gridData.width / CHUNK_MAX_SIZE; x++)
            for (int y = 0; y < gridData.height / CHUNK_MAX_SIZE; y++)
            {
                int chunkIndex = ChunkCoordsToIndex(x, y, gridData.width, CHUNK_MAX_SIZE);
                UpdateChunkVisual(gridData.chunkArray[chunkIndex]);
            }
    }

    /// <summary>
    /// Updates visualization using a specific flow-field index.
    /// </summary>
    public void UpdateGridVisual(GridData gridData, int flowFieldIndex)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        flowFieldIndex = Mathf.Max(flowFieldIndex, 0);

        if (showCells)
        {
            for (int x = 0; x < gridData.width; x++)
                for (int y = 0; y < gridData.height; y++)
                {
                    int cellIndex = CoordsToIndex(x, y, gridData.width);
                    Entity e = gridData.flowFieldArray[flowFieldIndex].gridCellEntityArray[cellIndex];
                    var cell = entityManager.GetComponentData<GridCell>(e);

                    UpdateCellVisual(cell);
                }
        }

        if (showChunks)
        {
            for (int x = 0; x < gridData.width / CHUNK_MAX_SIZE; x++)
                for (int y = 0; y < gridData.height / CHUNK_MAX_SIZE; y++)
                {
                    int chunkIndex = ChunkCoordsToIndex(x, y, gridData.width, CHUNK_MAX_SIZE);
                    UpdateChunkVisual(gridData.chunkArray[chunkIndex]);
                }
        }
    }

    /// <summary>
    /// Updates a single cell visualization.
    /// </summary>
    public void UpdateCellVisual(GridCell cell)
    {
        if (!showCells) return;

        var cellDebug = gridCellsArray[cell.x, cell.y];

        cellDebug.SetSpriteRotation(
            Quaternion.LookRotation(Vector3.right, Vector3.up));

        if (cell.stepCost == 0 && cell.bestPathCost == 0)
        {
            cellDebug.SetSprite(baseCell);
            cellDebug.SetColor(new Color(0.4f, 1f, 0.3f, 1));
            return;
        }

        if (cell.stepCost == OBSTRUCTED_COST)
        {
            cellDebug.SetSprite(baseCell);
            cellDebug.SetColor(Color.red);
            return;
        }

        if (cell.bestPathCost == int.MaxValue)
        {
            cellDebug.SetSprite(noPathCell);
            cellDebug.SetColor(new Color(1, 0, 0, .25f));
            return;
        }

        if (!cell.flowVector.Equals(Vector2.zero))
        {
            cellDebug.SetSprite(arrowCell);
            cellDebug.SetColor(new Color(1, 1, 1, .25f));

            cellDebug.SetSpriteRotation(
                Quaternion.LookRotation(
                    new Vector3(cell.flowVector.x, 0, cell.flowVector.y),
                    Vector3.up));

            return;
        }

        cellDebug.SetSprite(baseCell);
        cellDebug.SetColor(new Color(1, 1, 1, .25f));
    }

    /// <summary>
    /// Updates a single chunk visualization.
    /// </summary>
    public void UpdateChunkVisual(GridChunk chunk)
    {
        if (!showChunks) return;

        var chunkDebug = gridChunksArray[chunk.cx, chunk.cy];

        if (chunk.obstructed)
        {
            chunkDebug.SetSprite(baseChunk);
            chunkDebug.SetColor(Color.red);
            return;
        }

        chunkDebug.SetColor(chunk.visited
            ? new Color(0, 1, 0, .25f)
            : new Color(1, 1, 1, .25f));
    }
}