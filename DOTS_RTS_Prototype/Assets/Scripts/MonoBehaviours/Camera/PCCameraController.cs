using System;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Handles camera movement, rotation, and zoom input for a player-controlled RTS-style camera.
/// Delegates zoom and rotation smoothing to <see cref="CameraHandler"/>.
/// </summary>
public class PCCameraController : MonoBehaviour
{
    /// <summary>
    /// Speed at which the camera moves across the scene.
    /// </summary>
    [Header("Camera Settings")]
    [SerializeField]
    [Tooltip("Speed at which the camera moves across the scene.")]
    float cameraMovementSpeed = 30f;

    /// <summary>
    /// Speed at which the camera rotates around its vertical axis.
    /// </summary>
    [SerializeField]
    [Tooltip("Speed at which the camera rotates around its vertical axis.")]
    float cameraRotationSpeed = 1f;

    /// <summary>
    /// Amount of zoom applied per input step.
    /// </summary>
    [Header("Zoom Settings")]
    [SerializeField]
    [Tooltip("Amount of zoom applied per input step.")]
    float zoomStepMultiplier = 10f;

    /// <summary>
    /// Reference to the camera handler responsible for zoom, rotation, and grid clamping logic.
    /// </summary>
    [Header("References")]
    [SerializeField]
    private CameraHandler camHandler;

    /// <summary>
    /// Initializes required camera components and validates configuration.
    /// </summary>
    void Awake()
    {
        camHandler = gameObject.GetComponent<CameraHandler>();

        if (!camHandler || !camHandler.enabled)
        {
            Debug.LogError("Camera controller could not find CameraHandler component");
        }
    }

    /// <summary>
    /// Updates camera movement, rotation, and zoom input each frame.
    /// </summary>
    void Update()
    {
        HandleKeyboardCameraMovement();
        HandleKeyboardCameraRotation();
        HandleMouseWheelCameraZoom();
    }

    /// <summary>
    /// Processes keyboard input to move the camera relative to its current forward direction.
    /// </summary>
    private void HandleKeyboardCameraMovement()
    {
        Vector2 horizontalMoveDirection = GetMoveInput();

        Vector3 moveDirection = new Vector3(horizontalMoveDirection.x, 0, horizontalMoveDirection.y);
        Transform cameraTransform = Camera.main.transform;
        moveDirection = cameraTransform.forward * moveDirection.z + cameraTransform.right * moveDirection.x;
        moveDirection.y = 0;
        moveDirection.Normalize();

        transform.position += moveDirection * cameraMovementSpeed;
        camHandler.ClampToGridBounds();
    }

    /// <summary>
    /// Reads WASD input and converts it into a normalized 2D movement vector.
    /// </summary>
    /// <returns>A 2D directional input vector.</returns>
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

    /// <summary>
    /// Processes mouse scroll input and forwards zoom input to the camera handler.
    /// </summary>
    private void HandleMouseWheelCameraZoom()
    {
        float deltaZoom = Input.mouseScrollDelta.y * 10 * zoomStepMultiplier;
        camHandler.HandleZoom(deltaZoom);
    }

    /// <summary>
    /// Processes keyboard input for camera rotation around the vertical axis.
    /// </summary>
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