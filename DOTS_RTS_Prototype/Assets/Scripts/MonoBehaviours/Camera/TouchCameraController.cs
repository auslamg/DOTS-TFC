using System;
using System.Numerics;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

/// <summary>
/// Manages touch-based camera control with single-touch drag, two-finger pinch zoom, and twist rotation.
/// </summary>
public class TouchCameraController : MonoBehaviour
{
    [Header("Camera Settings")]

    /// <summary>
    /// Speed at which the camera moves across the scene during drag gestures.
    /// </summary>
    [SerializeField]
    [Tooltip("Speed at which the camera moves across the scene during drag gestures.")]
    float cameraMovementSpeed = 30f;

    /// <summary>
    /// Speed at which the camera rotates around its vertical axis during twist gestures.
    /// </summary>
    [SerializeField]
    [Tooltip("Speed at which the camera rotates around its vertical axis during twist gestures.")]
    float cameraRotationSpeed = 1f;

    [Header("Camera movement internal fields")]

    /// <summary>
    /// World position where the current drag gesture began.
    /// </summary>
    private Vector3 dragStartWorldPosition;

    /// <summary>
    /// Cumulative drag distance during the current drag gesture, used for inertia threshold calculation.
    /// </summary>
    private Vector3 totalDragVector;

    /// <summary>
    /// Current velocity vector for inertial momentum after drag release.
    /// </summary>
    private Vector2 inertialMovementVector;

    [Header("Movement inertia Settings")]

    /// <summary>
    /// Maximum velocity magnitude allowed for inertial momentum after drag release.
    /// </summary>
    [SerializeField]
    [Range(0, 100)]
    [Tooltip("Maximum velocity magnitude allowed for inertial momentum after drag release.")]
    private float inertiaMaximumVelocity = 10f;

    /// <summary>
    /// Minimum drag distance required to trigger inertial momentum on touch release.
    /// </summary>
    [SerializeField]
    [Range(0, 50)]
    [Tooltip("Minimum drag distance required to trigger inertial momentum on touch release.")]
    private float inertiaThresholdMinDistance = 1.5f;

    /// <summary>
    /// Friction multiplier applied per frame to decay inertial momentum over time.
    /// </summary>
    [SerializeField]
    [Range(0, 1f)]
    [Tooltip("Friction multiplier applied per frame to decay inertial momentum over time.")]
    private float inertiaFrictionFactor = 0.05f;

    /// <summary>
    /// Duration of debounce period that prevents new drag inputs from being processed to avoid input overlap between single-touch and two-finger gestures.
    /// </summary>
    [SerializeField]
    [Range(0.01f, 0.5f)]
    [Tooltip("Duration of debounce period that prevents new drag inputs from being processed to avoid input overlap between single-touch and two-finger gestures.")]
    float dragDebounceTime = 0.1f;

    /// <summary>
    /// Remaining debounce time; counts down each frame.
    /// </summary>
    float dragDebounceTimeCountdown = 0.1f;

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

    /// <summary>
    /// Target field of view angle the camera is currently transitioning toward via smoothing.
    /// </summary>
    private float targetFOV;

    [Header("References")]

    /// <summary>
    /// Cinemachine camera component used for controlling the view and focus.
    /// </summary>
    [SerializeField]
    [Tooltip("Cinemachine camera component used for controlling the view and focus.")]
    private CinemachineCamera cinemachineCamera;

    /// <summary>
    /// Number of active touches detected in the previous frame.
    /// Used to detect touch transitions (e.g., from 1 to 2 fingers).
    /// </summary>
    private int previousTouchCount = 0;

    /// <summary>
    /// Updates camera state based on current touch input every frame.
    /// Processes single-touch drag movement, two-finger pinch zoom, and two-finger twist rotation.
    /// </summary>
    void Update()
    {
        HandleTouchDragMovement();
        HandlePinchZoom();
        HandleTwistRotation();

        previousTouchCount = Input.touchCount;
    }

    /// <summary>
    /// Handles single-touch drag movement with inertial momentum.
    /// </summary>
    /// <remarks>
    /// Known Issue: View jitters when transitioning between one and two fingers because the second touch
    /// position is temporarily treated as the first touch, causing rapid displacement vectors.
    /// To fix: Add explicit touch tracking by ID rather than index.
    /// </remarks>
    private void HandleTouchDragMovement()
    {
        Vector2 horizontalMoveDirection = Vector2.zero;

        if (Input.touchCount == 1)
        {
            // If touch begins, save a start position and reset total drag and inertia vectors.
            Vector3 currentTouchWorldPosition = TouchWorldPosition.Instance.GetPosition(0);
            if (Input.GetTouch(0).phase == TouchPhase.Began || previousTouchCount != 1)
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

        if (dragDebounceTimeCountdown > 0)
        {
            dragDebounceTimeCountdown -= Time.deltaTime;
            return;
        }

        horizontalMoveDirection = AccountForInertia(horizontalMoveDirection);

        // Apply movement 
        Vector3 moveDirection = new Vector3(horizontalMoveDirection.x, 0, horizontalMoveDirection.y);
        transform.position += moveDirection * cameraMovementSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Applies inertial momentum decay to movement direction each frame.
    /// </summary>
    /// <param name="horizontalMoveDirection">The current horizontal movement direction from input.</param>
    /// <returns>Movement direction adjusted with inertial momentum contribution.</returns>
    private Vector2 AccountForInertia(Vector2 horizontalMoveDirection)
    {
        float inertiaSqrd = inertialMovementVector.sqrMagnitude;
        if (inertiaSqrd > 0)
        {
            // Maximum velocity limit.
            if (inertiaSqrd > Mathf.Pow(inertiaMaximumVelocity, 2))
            {
                inertialMovementVector.Normalize();
                inertialMovementVector *= inertiaMaximumVelocity;
            }

            // Avoid constant calculation with extremely small numbers.
            if (inertiaSqrd < 0.01f)
            {
                inertialMovementVector = Vector3.zero;
            }

            horizontalMoveDirection += inertialMovementVector;
            // Inertia friction: decrease inertia over time.
            inertialMovementVector *= Mathf.Pow(inertiaFrictionFactor, Time.deltaTime);
        }

        return horizontalMoveDirection;
    }

    /// <summary>
    /// Handles two-finger pinch movement (deprecated).
    /// </summary>
    /// <remarks>
    /// This feature was scrapped due to UX issues; pinch gesture is now used exclusively for zoom.
    /// </remarks>
    [Obsolete("This feature was scrapped due to UX issues.")]
    private void HandlePinchDragMovement()
    {
        /* if (dragDebounceTimeCountdown > 0)
        {
            dragDebounceTimeCountdown -= Time.deltaTime;
            return;
        } */

        if (Input.touchCount != 2) return;

        Vector2 horizontalMoveDirection = Vector2.zero;

        if (Input.touchCount == 2)
        {
            // If touch begins, save a start position and reset total drag and inertia vectors.
            Vector3 currentTouch0WorldPosition = TouchWorldPosition.Instance.GetPosition(0);
            Vector3 currentTouch1WorldPosition = TouchWorldPosition.Instance.GetPosition(1);

            //FIX Culprit
            Vector3 currentAvgWorldPosition =
                currentTouch0WorldPosition + currentTouch1WorldPosition * 0.5f;


            if (Input.GetTouch(0).phase == TouchPhase.Began &&
                Input.GetTouch(1).phase == TouchPhase.Began &&
                previousTouchCount != 2 &&
                previousTouchCount != 1)
            {
                dragStartWorldPosition = currentAvgWorldPosition;
                totalDragVector = Vector3.zero;
                inertialMovementVector = Vector2.zero;
                return;
            }

            // If dragging a touch, make the movement vector the opposite of the drag vector.
            Vector3 displacementVector = dragStartWorldPosition - currentAvgWorldPosition;
            horizontalMoveDirection =
                new Vector2(
                    x: displacementVector.x,
                    y: displacementVector.z);

            // Apply movement 
            Vector3 moveDirection = new Vector3(horizontalMoveDirection.x, 0, horizontalMoveDirection.y);
            transform.position += moveDirection * cameraMovementSpeed * Time.deltaTime;

            DebounceTouchDrag();
        }

        
    }

    /// <summary>
    /// Handles two-finger pinch zoom gesture by adjusting the camera's field of view.
    /// </summary>
    /// <remarks>
    /// Calculates the change in distance between two touch points and adjusts the target FOV accordingly,
    /// then smoothly interpolates toward the target FOV each frame.
    /// </remarks>
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
        float prevZoomDistance = (touch0PrevPos - touch1PrevPos).magnitude;
        float currentZoomDistance = (touch0CurrentPos - touch1CurrentPos).magnitude;

        // Difference = pinch amount
        float deltaZoom = currentZoomDistance - prevZoomDistance;

        // Initialize targetFOV if needed
        if (targetFOV == 0f)
            targetFOV = cinemachineCamera.Lens.FieldOfView;

        // Adjust target FOV
        targetFOV -= deltaZoom * zoomStepMultiplier * Time.deltaTime;
        targetFOV = Mathf.Clamp(targetFOV, minimumFOV, maximumFOV);
        // Smoothing
        cinemachineCamera.Lens.FieldOfView =
            Mathf.Lerp(cinemachineCamera.Lens.FieldOfView, targetFOV, 10 / zoomSmoothingMultiplier);

        // Avoid input overlap
        DebounceTouchDrag();
    }

    /// <summary>
    /// Handles two-finger twist gesture by rotating the camera around its vertical axis.
    /// </summary>
    /// <remarks>
    /// Computes the signed angle between the previous and current touch vectors to determine rotation amount.
    /// </remarks>
    private void HandleTwistRotation()
    {
        if (Input.touchCount != 2) return;

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        // Positions in current frame
        Vector2 touch0CurrentPos = touch0.position;
        Vector2 touch1CurrentPos = touch1.position;
        Vector2 centerCurrentPos = touch1CurrentPos - touch0CurrentPos;

        // Positions in previous frame
        Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
        Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
        Vector2 centerPrevPos = touch1PrevPos - touch0PrevPos;

        // Distance between touches
        Vector2 prevTouchVector = (touch0PrevPos - touch1PrevPos);
        Vector2 currentTouchVector = (touch0CurrentPos - touch1CurrentPos);
        float angle = Vector2.SignedAngle(prevTouchVector, currentTouchVector);

        // Difference = pinch amount
        transform.eulerAngles += new Vector3(0, angle * cameraRotationSpeed, 0);

        // Avoid input overlap
        DebounceTouchDrag();
    }

    /// <summary>
    /// Resets drag state and activates the debounce timer to prevent gesture interference.
    /// </summary>
    /// <remarks>
    /// Clears cumulative drag vectors and resets the debounce countdown. Called whenever
    /// multi-touch gestures (pinch or twist) are activated to prevent single-touch drag
    /// from interfering with two-finger inputs.
    /// </remarks>
    private void DebounceTouchDrag()
    {
        totalDragVector = Vector3.zero;
        inertialMovementVector = Vector2.zero;
        dragDebounceTimeCountdown = dragDebounceTime;
    }
}
