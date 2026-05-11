using Unity.Cinemachine;
using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    [Header("Zoom Settings")]

    /// <summary>
    /// Minimum field of view angle (in degrees) when zooming in.
    /// </summary>
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
    /// Smoothing speed for field of view interpolation toward the target zoom level. Higher values result in faster zoom transitions.
    /// </summary>
    [SerializeField]
    [Tooltip("Smoothing speed for field of view interpolation toward the target zoom level. Higher values result in faster zoom transitions.")]
    float zoomSmoothingMultiplier = 100;

    [Header("References")]

    /// <summary>
    /// Cinemachine camera component used for controlling the view and focus.
    /// </summary>
    [SerializeField]
    [Tooltip("Cinemachine camera component used for controlling the view and focus.")]
    private CinemachineCamera cinemachineCamera;

    /// <summary>
    /// Grid parameters ScriptableObject for camera move constraints.
    /// </summary>
    [SerializeField]
    [Tooltip("Grid parameters ScriptableObject for camera move constraints.")]
    private GridParametersSO gridParametersSO;

    /// <summary>
    /// Target field of view angle the camera is currently transitioning toward via smoothing.
    /// </summary>
    private float targetFOV;

    void Awake()
    {
        targetFOV = cinemachineCamera.Lens.FieldOfView;
    }

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

    public void HandleRotation(float deltaRotation)
    {
        transform.eulerAngles += new Vector3(0, deltaRotation, 0);
    }
}
