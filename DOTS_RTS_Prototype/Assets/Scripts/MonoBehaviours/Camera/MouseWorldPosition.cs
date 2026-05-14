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
    /// Global singleton instance for accessing mouse world-position services.
    /// </summary>
    public static MouseWorldPosition Instance { get; private set; }

    /// <summary>
    /// Chooses projection mode: physics raycast when true, flat plane when false.
    /// </summary>
    [SerializeField]
    [Tooltip("When enabled, uses Physics.Raycast; when disabled, projects to a flat Y=0 plane.")]
    private bool usePhysics = false;

    /// <summary>
    /// Key used to log the current mouse world position for debugging purposes.
    /// </summary>
    [SerializeField]
    [Tooltip("Key used to log the current mouse world position.")]
    private KeyCode debugKey = KeyCode.M;

    /// <summary>
    /// Ensures singleton instance validity and enforces a single active instance.
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

    /// <summary>
    /// Initializes the singleton instance during object initialization.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// Debug utility that logs the current mouse world position when the configured key is pressed.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            Debug.Log($"MouseWorldPosition: {GetPosition()}");
        }
    }

    /// <summary>
    /// Retrieves the mouse world position using the currently selected projection mode.
    /// </summary>
    /// <returns>
    /// The projected world position of the mouse, or <see cref="Vector3.zero"/> if projection fails.
    /// </returns>
    public Vector3 GetPosition()
    {
        return usePhysics ? GetPositionPhysics() : GetPositionFlat();
    }

    /// <summary>
    /// Projects the mouse position onto a flat horizontal plane at Y = 0.
    /// </summary>
    /// <remarks>
    /// This method avoids physics queries for performance and deterministic behavior,
    /// making it suitable for RTS-style terrain interaction.
    /// </remarks>
    /// <returns>
    /// The projected world position of the mouse, or <see cref="Vector3.zero"/> if projection fails.
    /// </returns>
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
    /// Projects the mouse position into the world using a physics raycast.
    /// </summary>
    /// <remarks>
    /// This method depends on scene colliders and may be more expensive than flat projection.
    /// </remarks>
    /// <returns>
    /// The hit point of the raycast, or <see cref="Vector3.zero"/> if no collider is hit.
    /// </returns>
    private Vector3 GetPositionPhysics()
    {
        Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

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