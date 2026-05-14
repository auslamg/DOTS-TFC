using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Controls camera movement, rotation, zoom, and grid boundary enforcement for a top-down or RTS-style camera system.
/// Integrates with Cinemachine for lens control and uses grid constraints to prevent out-of-bounds movement.
/// </summary>
public class CameraHandler : MonoBehaviour
{
    /// <summary>
    /// Minimum field of view angle (in degrees) when zooming in.
    /// </summary>
    [Header("Zoom Settings")]
    [SerializeField]
    [Tooltip("Minimum field of view angle when zooming in.")]
    float minimumFOV = 10f;

    /// <summary>
    /// Maximum field of view angle (in degrees) when zooming out.
    /// </summary>
    [SerializeField]
    [Tooltip("Maximum field of view angle when zooming out.")]
    float maximumFOV = 70f;

    /// <summary>
    /// Zoom sensitivity multiplier applied to the pinch distance delta per frame.
    /// </summary>
    [SerializeField]
    [Tooltip("Zoom sensitivity multiplier applied to the pinch distance delta per frame.")]
    float zoomStepMultiplier = 10f;

    /// <summary>
    /// Smoothing multiplier controlling how quickly the field of view interpolates toward the target zoom level.
    /// Higher values result in slower, smoother transitions.
    /// </summary>
    [SerializeField]
    [Tooltip("Smoothing speed for field of view interpolation toward the target zoom level. Higher values result in faster zoom transitions.")]
    float zoomSmoothingMultiplier = 100;

    /// <summary>
    /// Cinemachine camera component used for controlling the view and lens parameters.
    /// </summary>
    [Header("References")]
    [SerializeField]
    [Tooltip("Cinemachine camera component used for controlling the view and focus.")]
    private CinemachineCamera cinemachineCamera;

    /// <summary>
    /// Grid parameters used to define world bounds and camera movement constraints.
    /// </summary>
    [SerializeField]
    [Tooltip("Grid parameters ScriptableObject for camera move constraints.")]
    private GridParametersSO gridParametersSO;

    /// <summary>
    /// Target field of view the camera smoothly interpolates toward.
    /// </summary>
    private float targetFOV;

    /// <summary>
    /// Initializes the camera state and caches the initial field of view as the starting target value.
    /// </summary>
    void Awake()
    {
        targetFOV = cinemachineCamera.Lens.FieldOfView;
    }

    /// <summary>
    /// Clamps the camera position to the defined grid bounds to prevent out-of-range movement.
    /// </summary>
    public void ClampToGridBounds()
    {
        if (!GridUtil.ValidateCoords(
                GridUtil.WorldPositionToCoords(transform.position, gridParametersSO.gridCellSize),
                gridParametersSO.size))
        {
            Debug.Log("Clamping position");
            float x = transform.position.x;
            float z = transform.position.z;

            transform.position = new Vector3(
                x: Mathf.Clamp(x, 0, gridParametersSO.gridCellSize * gridParametersSO.size),
                y: transform.position.y,
                z: Mathf.Clamp(z, 0, gridParametersSO.gridCellSize * gridParametersSO.size));
        }
    }

    /// <summary>
    /// Applies zoom input by adjusting the target field of view and smoothing the transition.
    /// </summary>
    /// <param name="deltaZoom">Input zoom delta value (positive or negative).</param>
    public void HandleZoom(float deltaZoom)
    {
        // Initialize targetFOV if needed
        if (targetFOV == 0f)
            targetFOV = cinemachineCamera.Lens.FieldOfView;

        // Adjust target FOV
        targetFOV -= deltaZoom * Time.deltaTime;
        targetFOV = Mathf.Clamp(targetFOV, minimumFOV, maximumFOV);

        // Smoothing
        cinemachineCamera.Lens.FieldOfView =
            Mathf.Lerp(cinemachineCamera.Lens.FieldOfView, targetFOV, 10 / zoomSmoothingMultiplier);
    }

    /// <summary>
    /// Rotates the camera around the Y-axis based on input rotation delta.
    /// </summary>
    /// <param name="deltaRotation">Rotation amount in degrees to apply this frame.</param>
    public void HandleRotation(float deltaRotation)
    {
        transform.eulerAngles += new Vector3(0, deltaRotation, 0);
    }
}