using Unity.Cinemachine;
using UnityEngine;

//TODO: Document
/// <summary>
/// Handles camera movement, rotation, and zoom.
/// </summary>
public class PCCameraController : MonoBehaviour
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
    float cameraRotationSpeed = 1f;

    [Header("Zoom Settings")]

    /// <summary>
    /// Amount of zoom applied per input step.
    /// </summary>
    [SerializeField]
    [Tooltip("Amount of zoom applied per input step.")]
    float zoomStepMultiplier = 10f;
    

    [Header("References")]

    /// <summary>
    /// Cinemachine camera used for controlling the view.
    /// </summary>
    [SerializeField]
    [Tooltip("Cinemachine camera used for controlling the view.")]
    private CinemachineCamera cinemachineCamera;

    [SerializeField]
    private CameraHandler camHandler;


    void Awake()
    {
        camHandler = gameObject.GetComponent<CameraHandler>();

        if (!camHandler.enabled || camHandler == null)
        {
            Debug.LogError("Camera controller could not find ZoomHandler component");
        }
    }

    // Update is called once per frame
    void Update()
    {
        HandleKeyboardCameraMovement();
        HandleKeyboardCameraRotation();
        HandleMouseWheelCameraZoom();
    }

    private void HandleKeyboardCameraMovement()
    {
        Vector2 horizontalMoveDirection = GetMoveInput();

        Vector3 moveDirection = new Vector3(horizontalMoveDirection.x, 0, horizontalMoveDirection.y);
        Transform cameraTransform = Camera.main.transform;
        moveDirection = cameraTransform.forward * moveDirection.z + cameraTransform.right * moveDirection.x;
        moveDirection.y = 0;
        moveDirection.Normalize();

        transform.position += moveDirection * cameraMovementSpeed * Time.deltaTime;
    }

    private static Vector2 GetMoveInput()
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

        return horizontalMoveDirection;
    }

    private void HandleMouseWheelCameraZoom()
    {
        float deltaZoom = Input.mouseScrollDelta.y * 10 * zoomStepMultiplier;

        camHandler.HandleZoom(deltaZoom);
    }

    private void HandleKeyboardCameraRotation()
    {
        float deltaRotation = 0;
        if (Input.GetKey(KeyCode.Q))
        {
            deltaRotation += cameraRotationSpeed;
        }
        if (Input.GetKey(KeyCode.E))
        {
            deltaRotation -= cameraRotationSpeed;
        }

        camHandler.HandleRotation(deltaRotation);
    }
}
