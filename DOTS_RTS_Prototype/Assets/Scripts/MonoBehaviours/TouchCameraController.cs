using System;
using System.Numerics;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class TouchCameraController : MonoBehaviour
{
    [Header("Camera Settings")]

    //FIX Remove or replace by "proportion" 
    /// <summary>
    /// Speed at which the camera moves across the scene.
    /// </summary>
    [SerializeField]
    [Tooltip("Speed at which the camera moves across the scene.")]
    float cameraMovementSpeed = 30f;

    //FIX Remove or replace by "proportion" 
    /// <summary>
    /// Speed at which the camera rotates around its vertical axis.
    /// </summary>
    [SerializeField]
    [Tooltip("Speed at which the camera rotates around its vertical axis.")]
    float cameraRotationSpeed = 1f;

    [Header("Camera movement internal fields")]
    private Vector3 dragStartWorldPosition;

    private Vector3 totalDragVector;

    private Vector2 inertialMovementVector;

    [Header("Movement inertia Settings")]
    [SerializeField]
    [Range(0, 100)]
    private float inertiaMaximumVelocity = 10f;

    [SerializeField]
    [Range(0, 50)]
    private float inertiaThresholdMinDistance = 1.5f;

    [SerializeField]
    [Range(0, 1f)]
    private float inertiaFrictionFactor = 0.05f;

    [SerializeField]
    [Range(0.01f, 0.2f)]
    float dragDebounceTime = 0.1f;

    [SerializeField, HideInInspector]
    float dragDebounceTimeCountdown = 0.1f;

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
    float maximumFOV = 70f;

    //FIX Remove or replace by "proportion" 
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
    float zoomSmoothingMultiplier = 100;

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


    // Update is called once per frame
    void Update()
    {
        HandleTouchDragMovement();
        HandlePinchZoom();
    }

    private void HandleTouchDragMovement()
    {
        if (dragDebounceTimeCountdown > 0)
        {
            dragDebounceTimeCountdown -= Time.deltaTime;
            return;
        }

        Vector2 horizontalMoveDirection = Vector2.zero;

        if (Input.touchCount == 1)
        {
            // If touch begins, save a start position and reset total drag and inertia vectors.
            Vector3 currentTouchWorldPosition = TouchWorldPosition.Instance.GetPosition(0);
            if (Input.GetTouch(0).phase == TouchPhase.Began)
            {
                dragStartWorldPosition = currentTouchWorldPosition;
                totalDragVector = Vector3.zero;
                inertialMovementVector = Vector2.zero;
                return;
            }

            // If dragging a touch, make the movement vector the opposite of the drag vector.
            Vector3 displacementVector = dragStartWorldPosition - currentTouchWorldPosition;
            horizontalMoveDirection =
                new Vector2(
                    x: displacementVector.x,
                    y: displacementVector.z);

            // Compute dragged distance into total for inertia threshold.
            totalDragVector += displacementVector;
            // If releasing the drag with a distance above the minimum, make the inertia vector the current movement vector.
            if (Input.GetTouch(0).phase == TouchPhase.Ended &&
                totalDragVector.magnitude > inertiaThresholdMinDistance)
            {
                inertialMovementVector = horizontalMoveDirection;
            }
        }

        // Inertia handling.
        if (inertialMovementVector.magnitude > 0)
        {
            if (inertialMovementVector.sqrMagnitude > Mathf.Pow(inertiaMaximumVelocity, 2))
            {
                inertialMovementVector.Normalize();
                inertialMovementVector *= inertiaMaximumVelocity;
            }

            horizontalMoveDirection += inertialMovementVector;
            // Inertia friction: decrease inertia over time.
            inertialMovementVector *= Mathf.Pow(inertiaFrictionFactor, Time.deltaTime);
        }



        // Apply movement 
        Vector3 moveDirection = new Vector3(horizontalMoveDirection.x, 0, horizontalMoveDirection.y);
        transform.position += moveDirection * cameraMovementSpeed * Time.deltaTime;
    }





    private void HandlePinchZoom()
    {
        if (Input.touchCount != 2) return;

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        // Positions in current frame
        Vector2 touch0CurrentPos = touch0.position;
        Vector2 touch1CurrentPos = touch1.position;

        // Positions in previous frame
        Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
        Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

        // Distance between touches
        float prevTouchDeltaMag = (touch0PrevPos - touch1PrevPos).magnitude;
        float currentTouchDeltaMag = (touch0CurrentPos - touch1CurrentPos).magnitude;

        // Difference = pinch amount
        float deltaMagnitude = currentTouchDeltaMag - prevTouchDeltaMag;

        // Initialize targetFOV if needed
        if (targetFOV == 0f)
            targetFOV = cinemachineCamera.Lens.FieldOfView;

        // Adjust target FOV (invert if needed)
        targetFOV -= deltaMagnitude * zoomStepMultiplier * Time.deltaTime;

        // Clamp zoom
        targetFOV = Mathf.Clamp(targetFOV, minimumFOV, maximumFOV);

        // Smooth transition
        cinemachineCamera.Lens.FieldOfView = Mathf.Lerp(
            cinemachineCamera.Lens.FieldOfView,
            targetFOV,
            zoomSmoothingMultiplier * Time.deltaTime
        );

        totalDragVector = Vector2.zero;
        inertialMovementVector = Vector2.zero;
        dragDebounceTimeCountdown = dragDebounceTime;
    }
}
