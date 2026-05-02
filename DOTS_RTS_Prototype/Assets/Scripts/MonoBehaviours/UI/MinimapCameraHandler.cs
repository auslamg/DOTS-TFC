using UnityEngine;

/// <summary>
/// Manages minimap camera setup and icon scaling based on grid dimensions.
/// </summary>
/// <remarks>
/// This component is a singleton that handles camera positioning, orthographic size configuration,
/// and icon scaling multipliers based on the grid data. It provides managed-side access to the minimap
/// camera's icon size multiplier for UI rendering systems.
/// </remarks>
public class MinimapCameraHandler : MonoBehaviour
{
    /// <summary>
    /// Multiplier used to scale minimap icons relative to grid dimensions.
    /// </summary>
    [SerializeField]
    [Tooltip("Multiplier used to scale minimap icons relative to grid dimensions.")]
    float iconSizeMultiplier = 1;

    /// <summary>
    /// Scene singleton instance for managed-side access.
    /// </summary>
    public static MinimapCameraHandler Instance { get; private set; }

    /// <summary>
    /// Gets the current icon size multiplier for UI rendering.
    /// </summary>
    public float GetIconSizeMultiplier { get => iconSizeMultiplier; }

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
    
    /// <summary>
    /// Initializes the minimap camera position, size, and icon scaling based on grid dimensions.
    /// </summary>
    /// <param name="gridData">Grid data containing width, height, and cell size information.</param>
    public void InitializeCamera(GridData gridData)
    {
        // Decompose gridData
        int gridWidth = gridData.width;
        int gridHeight = gridData.height;
        float gridCellSize = gridData.gridCellSize;

        // Get map dimensions
        float mapWidth = gridWidth * gridCellSize;
        float mapHeight = gridHeight * gridCellSize;

        // Move camera to center of the map.
        Vector3 mapCameraPosition = new Vector3(
            x: mapWidth * 0.5f,
            y: transform.position.y,
            z: mapHeight * 0.5f);
        transform.position = mapCameraPosition;
        
        // Set ortographic size (proportional to cell size and grid proportions).
        Camera cam = gameObject.GetComponent<Camera>();
        cam.orthographicSize = Mathf.Max(mapWidth,mapHeight) / 2;

        // Set icon size 
        iconSizeMultiplier = gridWidth;
    }
}
