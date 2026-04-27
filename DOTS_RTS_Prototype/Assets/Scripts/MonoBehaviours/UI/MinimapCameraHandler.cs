using UnityEngine;

public class MinimapCameraHandler : MonoBehaviour
{
    bool centered = false;

    [SerializeField] float iconSizeMultiplier = 1;

    /// <summary>
    /// Scene singleton instance for managed-side access.
    /// </summary>
    public static MinimapCameraHandler Instance { get; private set; }
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
