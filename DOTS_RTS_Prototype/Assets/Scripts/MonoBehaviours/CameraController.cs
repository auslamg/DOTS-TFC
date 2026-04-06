using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Handles camera movement, rotation, and zoom.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]

    /// <summary>
    /// Speed at which the camera moves across the scene.
    /// </summary>
    [SerializeField]
    [Tooltip("Speed at which the camera moves across the scene.")]
    float cameraMovementSpeed = 30f;

    /// <summary>
    /// Speed at which the camera rotates around its vertical axis.
    /// </summary>
    [SerializeField]
    [Tooltip("Speed at which the camera rotates around its vertical axis.")]
    float cameraRotationSpeed = 200f;

    [Header("Zoom Settings")]

    /// <summary>
    /// Minimum field of view angle when zooming in.
    /// </summary>
    [SerializeField]
    [Tooltip("Minimum field of view angle when zooming in.")]
    float minimumFOV = 10f;

    /// <summary>
    /// Maximum field of view angle when zooming out.
    /// </summary>
    [SerializeField]
    [Tooltip("Maximum field of view angle when zooming out.")]
    float maximumFOV = 120f;  

    /// <summary>
    /// Amount of zoom applied per input step.
    /// </summary>
    [SerializeField]
    [Tooltip("Amount of zoom applied per input step.")]
    float zoomStepMultiplier = 10f;

    /// <summary>
    /// Rate at which the camera transitions to the target zoom level.
    /// </summary>
    [SerializeField]
    [Tooltip("Rate at which the camera transitions to the target zoom level.")]
    float zoomSmoothingMultiplier = 10;

    /// <summary>
    /// Desired field of view the camera moves toward when zooming.
    /// </summary>
    private float targetFOV;

    [Header("References")]

    /// <summary>
    /// Cinemachine camera used for controlling the view.
    /// </summary>
    [SerializeField]
    [Tooltip("Cinemachine camera used for controlling the view.")]
    private CinemachineCamera cinemachineCamera;


    void Awake()
    {
        targetFOV = cinemachineCamera.Lens.FieldOfView;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Vector2 horizontalMoveDirection = Vector2.zero;
        if (Input.GetKey(KeyCode.W))
        {
            horizontalMoveDirection.y += 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            horizontalMoveDirection.x += 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            horizontalMoveDirection.y -= 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            horizontalMoveDirection.x -= 1;
        }
        horizontalMoveDirection.Normalize();

        Vector3 moveDirection = new Vector3(horizontalMoveDirection.x, 0, horizontalMoveDirection.y);
        Transform cameraTransform = Camera.main.transform;
        moveDirection = cameraTransform.forward * moveDirection.z + cameraTransform.right * moveDirection.x;
        moveDirection.y = 0;
        moveDirection.Normalize();

        transform.position += moveDirection * cameraMovementSpeed * Time.deltaTime;

        float rotationTotal = 0;
        if (Input.GetKey(KeyCode.Q))
        {
            rotationTotal += 1;
        }
        if (Input.GetKey(KeyCode.E))
        {
            rotationTotal -= 1;
        }

        transform.eulerAngles += new Vector3(0, rotationTotal, 0);

        if (Input.mouseScrollDelta.y > 0)
        {
            targetFOV -= zoomStepMultiplier / 100 * cinemachineCamera.Lens.FieldOfView;
        }
        if (Input.mouseScrollDelta.y < 0)
        {
            targetFOV += zoomStepMultiplier / 100 * cinemachineCamera.Lens.FieldOfView;
        }

        targetFOV = Mathf.Clamp(targetFOV, minimumFOV, maximumFOV);
        cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(cinemachineCamera.Lens.FieldOfView, targetFOV, zoomSmoothingMultiplier);
    }
}
