using UnityEngine;

public class TouchWorldPosition : MonoBehaviour
{
    /// <summary>
    /// Global singleton access to touch world-position services.
    /// </summary>
    public static TouchWorldPosition Instance { get; private set; }

    /// <summary>
    /// Chooses projection mode: physics raycast when true, flat plane when false.
    /// </summary>
    [SerializeField]
    [Tooltip("When enabled, uses Physics.Raycast; when disabled, projects to a flat Y=0 plane.")]
    private bool usePhysics = false;

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
    /// Returns the world position of the first touch using the currently selected projection mode.
    /// </summary>
    /// <returns>Projected touch world position, or <see cref="Vector3.zero"/> when projection fails.</returns>
    public Vector3 GetPosition()
    {
        return GetPosition(Input.GetTouch(0));
    }

    /// <summary>
    /// Returns the world position of the touch at the specified index using the currently selected projection mode.
    /// </summary>
    /// <returns>Projected touch world position, or <see cref="Vector3.zero"/> when projection fails.</returns>
    public Vector3 GetPosition(int touchIndex)
    {
        return GetPosition(Input.GetTouch(touchIndex));
    }

    /// <summary>
    /// Returns the world position of the specified touch using the currently selected projection mode.
    /// </summary>
    /// <returns>Projected touch world position, or <see cref="Vector3.zero"/> when projection fails.</returns>
    public Vector3 GetPosition(Touch touch)
    {
        return usePhysics ? GetPositionPhysics(touch) : GetPositionFlat(touch);
    }

    /// <summary>
    /// Returns touch world position projected onto a mathematical flat ground plane.
    /// </summary>
    /// <remarks>
    /// Uses a plane intersection instead of physics for performance and to avoid DOTS/Physics overlap concerns.
    /// </remarks>
    /// <returns>Projected touch world position, or <see cref="Vector3.zero"/> when projection fails.</returns>
    private Vector3 GetPositionFlat(Touch touch)
    {
        Ray touchCameraRay = Camera.main.ScreenPointToRay(touch.position);
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(touchCameraRay, out float distance))
        {
            return touchCameraRay.GetPoint(distance);
        }
        else
        {
            return Vector3.zero;
        }
    }

    /// <summary>
    /// Returns touch world position using a physics raycast against scene colliders.
    /// </summary>
    /// <returns>Raycast hit point, or <see cref="Vector3.zero"/> when no collider is hit.</returns>
    private Vector3 GetPositionPhysics(Touch touch)
    {
        Ray touchCameraRay = Camera.main.ScreenPointToRay(touch.position);

        //TODO: Add layerMask

        if (Physics.Raycast(touchCameraRay, out RaycastHit hit))
        {
            return hit.point;
        }
        else
        {
            return Vector3.zero;
        }

    }
}
