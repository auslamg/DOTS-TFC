using Unity.Entities;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using static GridSystem;
using static GridUtil;

public class GridDebugDisplay : MonoBehaviour
{
    [SerializeField]
    private Transform gridCellGizmo;
    [SerializeField]
    private Transform gridChunkGizmo;
    [SerializeField]
    private Sprite baseCell;
    [SerializeField]
    private Sprite arrowCell;
    [SerializeField]
    private Sprite noPathCell;
    [SerializeField]
    private Sprite baseChunk;

    private bool isInitialized = false;
    private GridCellDebug[,] gridCellsArray;
    private GridChunkDebug[,] gridChunksArray;

    /// <summary>
    /// Scene singleton instance for managed-side access.
    /// </summary>
    public static GridDebugDisplay Instance { get; private set; }

    /// <summary>
    /// Initializes singleton instance state.
    /// </summary>
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple instances of singleton found on " + this.gameObject.name);
            Destroy(this);
        }
    }

    private void Awake()
    {
        InitializeSingleton();
    }

    public void InitializeGrid(GridData gridData)
    {
        gridCellsArray = new GridCellDebug[gridData.width, gridData.height];
        for (int x = 0; x < gridData.width; x++)
        {
            for (int y = 0; y < gridData.height; y++)
            {
                Transform cellGizmo = Instantiate(gridCellGizmo, this.gameObject.transform);
                cellGizmo.name = $"CellGizmo-{x},{y}";
                GridCellDebug cell = cellGizmo.GetComponent<GridCellDebug>();
                cell.Initialize(x, y, gridData.gridCellSize);

                gridCellsArray[x, y] = cell;
            }
        }

        gridChunksArray = new GridChunkDebug[gridData.width / CHUNK_MAX_SIZE, gridData.height / CHUNK_MAX_SIZE];
        for (int cx = 0; cx < gridData.width / CHUNK_MAX_SIZE; cx++)
        {
            for (int cy = 0; cy < gridData.width / CHUNK_MAX_SIZE; cy++)
            {
                Transform chunkGizmo = Instantiate(gridChunkGizmo, this.gameObject.transform);
                chunkGizmo.name = $"ChunkGizmo-{cx},{cy}";
                GridChunkDebug chunk = chunkGizmo.GetComponent<GridChunkDebug>();
                chunk.Initialize(cx, cy, gridData.gridCellSize, CHUNK_MAX_SIZE);

                gridChunksArray[cx, cy] = chunk;
            }
        }

        isInitialized = true;
    }

    //IDEA: Use jobs for this (maybe)
    public void UpdateGridVisual(GridData gridData)
    {
        for (int x = 0; x < gridData.width; x++)
        {
            for (int y = 0; y < gridData.height; y++)
            {
                // Retrieve the unmanaged data for this cell
                EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                int latestFlowFieldIndex = gridData.nextFlowFieldIndex - 1;
                if (latestFlowFieldIndex <= 0)
                {
                    latestFlowFieldIndex = 0;
                } // IDEA Use LoopCounter utility struct or similar

                int cellIndex = CoordsToIndex(x, y, gridData.width);
                Entity cellEntity = gridData.flowFieldArray[latestFlowFieldIndex].gridCellEntityArray[cellIndex];
                GridCell cell = entityManager.GetComponentData<GridCell>(cellEntity);

                UpdateCellVisual(cell);
            }
        }

        for (int x = 0; x < gridData.width / CHUNK_MAX_SIZE; x++)
        {
            for (int y = 0; y < gridData.height / CHUNK_MAX_SIZE; y++)
            {
                // Retrieve the unmanaged data for this cell
                EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

                int chunkIndex = ChunkCoordsToIndex(x, y, gridData.width, CHUNK_MAX_SIZE);
                GridChunk gridChunk = gridData.chunkArray[chunkIndex];

                UpdateChunkVisual(gridChunk);
            }
        }
    }

    public void UpdateGridVisual(GridData gridData, int flowFieldIndex)
    {
        for (int x = 0; x < gridData.width; x++)
        {
            for (int y = 0; y < gridData.height; y++)
            {
                // Retrieve the unmanaged data for this cell
                EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                if (flowFieldIndex <= 0)
                {
                    flowFieldIndex = 0;
                }

                int cellIndex = CoordsToIndex(x, y, gridData.width);
                Entity cellEntity = gridData.flowFieldArray[flowFieldIndex].gridCellEntityArray[cellIndex];
                GridCell cell = entityManager.GetComponentData<GridCell>(cellEntity);

                UpdateCellVisual(cell);
            }
        }

        for (int x = 0; x < gridData.width / CHUNK_MAX_SIZE; x++)
        {
            for (int y = 0; y < gridData.height / CHUNK_MAX_SIZE; y++)
            {
                // Retrieve the unmanaged data for this cell
                EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

                int chunkIndex = ChunkCoordsToIndex(x, y, gridData.width, CHUNK_MAX_SIZE);
                GridChunk gridChunk = gridData.chunkArray[chunkIndex];

                UpdateChunkVisual(gridChunk);
            }
        }
    }

    public void UpdateCellVisual(GridCell cell)
    {
        GridCellDebug cellDebug = gridCellsArray[cell.x, cell.y];

        cellDebug.SetSpriteRotation(Quaternion.LookRotation(
                    new Vector3(
                        1,
                        0,
                        0),
                    Vector3.up));

        if (cell.stepCost == 0 && cell.bestPathCost == 0) // Target
        {
            cellDebug.SetSprite(baseCell);
            cellDebug.SetColor(new Color(1, 1, 0, 1));
        }
        else
        {
            if (cell.stepCost == OBSTRUCTED_COST)
            {
                cellDebug.SetSprite(baseCell);
                cellDebug.SetColor(Color.red);
            }
            else if (cell.bestPathCost == int.MaxValue)
            {
                cellDebug.SetSprite(noPathCell);
                cellDebug.SetColor(new Color(1, 0, 0, .25f));
            }
            else if (!cell.flowVector.Equals(Vector2.zero))
            {
                cellDebug.SetSprite(arrowCell);
                cellDebug.SetColor(new Color(1, 1, 1, .25f));

                cellDebug.SetSpriteRotation(Quaternion.LookRotation(
                    new Vector3(
                        cell.flowVector.x,
                        0,
                        cell.flowVector.y),
                    Vector3.up));
            }
            else
            {
                cellDebug.SetSprite(baseCell);
                cellDebug.SetColor(new Color(1, 1, 1, .25f));
            }
        }
    }

    public void UpdateChunkVisual(GridChunk chunk)
    {
        GridChunkDebug chunkDebug = gridChunksArray[chunk.cx, chunk.cy];
        if (chunk.obstructed) // Target
        {
            chunkDebug.SetSprite(baseChunk);
            chunkDebug.SetColor(Color.red);
        }
        else
        {
            if (chunk.visited)
            {
                chunkDebug.SetColor(new Color(0, 1, 0, .25f));
            }
            else
            {
                chunkDebug.SetColor(new Color(1, 1, 1, .25f));
            }
        }
    }
}
