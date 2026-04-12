using UnityEngine;

/// <summary>
/// Provides mouse-to-world projection helpers for gameplay interactions.
/// </summary>
/// <remarks>
/// Supports two projection modes:
/// - Flat plane projection for fast RTS-style terrain.
/// - Physics raycast projection for complex terrain.
/// </remarks>
public class MouseWorldPosition : MonoBehaviour
{
    /// <summary>
    /// Global singleton access to mouse world-position services.
    /// </summary>
    public static MouseWorldPosition Instance { get; private set; }

    /// <summary>
    /// Chooses projection mode: physics raycast when true, flat plane when false.
    /// </summary>
    [SerializeField]
    [Tooltip("When enabled, uses Physics.Raycast; when disabled, projects to a flat Y=0 plane.")]
    private bool usePhysics = false;

    /// <summary>
    /// Key used to log the current mouse world position.
    /// </summary>
    [SerializeField]
    [Tooltip("Key used to log the current mouse world position.")]
    private KeyCode debugKey = KeyCode.M;

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
    /// Debug hook that logs the current mouse world position when pressing the debug key.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            Debug.Log(GetPosition());
        }
    }

    /// <summary>
    /// Returns the mouse world position using the currently selected projection mode.
    /// </summary>
    /// <returns>Projected mouse world position, or <see cref="Vector3.zero"/> when projection fails.</returns>
    public Vector3 GetPosition()
    {
        return usePhysics ? GetPositionPhysics() : GetPositionFlat();
    }

    /// <summary>
    /// Returns mouse world position projected onto a mathematical flat ground plane.
    /// </summary>
    /// <remarks>
    /// Uses a plane intersection instead of physics for performance and to avoid DOTS/Physics overlap concerns.
    /// </remarks>
    /// <returns>Projected mouse world position, or <see cref="Vector3.zero"/> when projection fails.</returns>
    private Vector3 GetPositionFlat()
    {
        Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(mouseCameraRay, out float distance))
        {
            return mouseCameraRay.GetPoint(distance);
        }
        else
        {
            return Vector3.zero;
        }
    }

    /// <summary>
    /// Returns mouse world position using a physics raycast against scene colliders.
    /// </summary>
    /// <returns>Raycast hit point, or <see cref="Vector3.zero"/> when no collider is hit.</returns>
    private Vector3 GetPositionPhysics()
    {
        Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        //TODO: Add layerMask

        if (Physics.Raycast(mouseCameraRay, out RaycastHit hit))
        {
            return hit.point;
        }
        else
        {
            return Vector3.zero;
        }

    }
}
