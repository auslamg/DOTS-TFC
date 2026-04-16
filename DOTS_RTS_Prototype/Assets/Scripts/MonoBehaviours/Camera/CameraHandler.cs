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
    /// Target field of view angle the camera is currently transitioning toward via smoothing.
    /// </summary>
    private float targetFOV;

    void Awake()
    {
        targetFOV = cinemachineCamera.Lens.FieldOfView;
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
