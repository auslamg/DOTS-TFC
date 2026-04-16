using Unity.Entities;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using static GridSystem;

public class GridDebugDisplay : MonoBehaviour
{
    [SerializeField]
    private Transform gridCellGizmo;
    [SerializeField]
    private Sprite baseCell;
    [SerializeField]
    private Sprite arrowCell;

    private bool isInitialized = false;
    private GridCellDebug[,] gridCellsArray;

    /// <summary>
    /// Scene singleton instance for managed-side access.
    /// </summary>
    public static GridDebugDisplay Instance { get; private set; }

    /// <summary>
    /// Initializes singleton instance state.
    /// </summary>
    void Awake()
    {
        // Initialize singleton instance state.
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

    public void InitializeGrid(GridData gridData)
    {
        gridCellsArray = new GridCellDebug[gridData.width, gridData.height];
        for (int x = 0; x < gridData.width; x++)
        {
            for (int y = 0; y < gridData.height; y++)
            {
                Transform cellGizmo = Instantiate(gridCellGizmo, this.gameObject.transform);
                GridCellDebug cell = cellGizmo.GetComponent<GridCellDebug>();
                cell.Initialize(x, y, gridData.gridCellSize);

                gridCellsArray[x, y] = cell;
            }
        }
        isInitialized = true;
    }

    //TODO: Use jobs for this, and don't update every frame

    public void UpdateGridVisual(GridData gridData)
    {
        for (int x = 0; x < gridData.width; x++)
        {
            for (int y = 0; y < gridData.height; y++)
            {
                // Retrieve the unmanaged data for this cell
                EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                int flowFieldIndex = gridData.nextFlowFieldIndex - 1;
                if (flowFieldIndex <= 0)
                {
                    flowFieldIndex = 0;
                } // IDEA Use LoopCounter utility struct or similar

                int cellIndex = GridSystem.CoordsToIndex(x, y, gridData.width);
                Entity cellEntity = gridData.gridMapArray[flowFieldIndex].gridCellEntityArray[cellIndex];
                GridCell cell = entityManager.GetComponentData<GridCell>(cellEntity);

                UpdateCellVisual(cell);
            }
        }
    }

    public void UpdateCellVisual(GridCell cell)
    {
        GridCellDebug cellDebug = gridCellsArray[cell.x, cell.y];
        if (cell.stepCost == 0) // Target
        {
            cellDebug.SetSprite(baseCell);
            cellDebug.SetColor(Color.yellow);
        }
        else
        {
            if (cell.stepCost == WALL_COST)
            {
                cellDebug.SetSprite(baseCell);
                cellDebug.SetColor(Color.red);
            }
            else
            {
                cellDebug.SetSprite(arrowCell);
                cellDebug.SetColor(Color.white);

                cellDebug.SetSpriteRotation(Quaternion.LookRotation(
                    new Vector3(
                        cell.pathingVector.x,
                        0,
                        cell.pathingVector.y),
                    Vector3.up));
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
