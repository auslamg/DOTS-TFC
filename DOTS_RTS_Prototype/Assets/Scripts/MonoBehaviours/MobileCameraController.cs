using Unity.Cinemachine;
using UnityEngine;

public class MobileCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] float cameraMovementSpeed = 30f;
    [SerializeField] float cameraRotationSpeed = 1f;

    [Header("Zoom Settings")]
    [SerializeField] float minimumFOV = 10f;
    [SerializeField] float maximumFOV = 70f;
    [SerializeField] float zoomStepMultiplier = 1f;
    [SerializeField] float zoomSmoothingMultiplier = 10f;

    private float targetFOV;

    [Header("References")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    private const float minPinchDistance = 10f;

    // Zoom state
    private float previousPinchDistance = -1f;
    private float previousTouchAngle;

    // Drag state
    private Vector2 lastTouchPosition;
    private bool isDragging;

    void Awake()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = GetComponent<CinemachineCamera>();

        targetFOV = cinemachineCamera.Lens.FieldOfView;
    }

    void Update()
    {
        HandleSingleTouchMovement();
        HandleTwoFingerTouchGestures();

        // Smooth zoom every frame
        cinemachineCamera.Lens.FieldOfView =
            Mathf.Lerp(
                cinemachineCamera.Lens.FieldOfView,
                targetFOV,
                Time.deltaTime * zoomSmoothingMultiplier
            );
    }

    // -----------------------------
    // TAP + DRAG MOVEMENT (FIXED)
    // -----------------------------
    private void HandleSingleTouchMovement()
    {
        if (Input.touchCount != 1)
        {
            isDragging = false;
            return;
        }

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            lastTouchPosition = touch.position;
            isDragging = true;
            return;
        }

        if (!isDragging)
            return;

        if (touch.phase == TouchPhase.Moved)
        {
            Vector2 delta = touch.position - lastTouchPosition;

            Transform cam = Camera.main.transform;

            Vector3 move =
                -cam.right * delta.x -
                cam.forward * delta.y;

            move.y = 0f;

            transform.position += move * cameraMovementSpeed * Time.deltaTime;

            lastTouchPosition = touch.position;
        }

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            isDragging = false;
        }
    }

    // -----------------------------
    // PINCH + TWIST GESTURES
    // -----------------------------
    private void HandleTwoFingerTouchGestures()
    {
        if (Input.touchCount != 2)
        {
            previousPinchDistance = -1f;
            previousTouchAngle = 0f;
            return;
        }

        Touch a = Input.GetTouch(0);
        Touch b = Input.GetTouch(1);

        Vector2 aPos = a.position;
        Vector2 bPos = b.position;

        float currentDistance = Vector2.Distance(aPos, bPos);
        float currentAngle =
            Mathf.Atan2(bPos.y - aPos.y, bPos.x - aPos.x) * Mathf.Rad2Deg;

        if (previousPinchDistance < 0f)
        {
            previousPinchDistance = currentDistance;
            previousTouchAngle = currentAngle;
            return;
        }

        float distanceDelta = currentDistance - previousPinchDistance;
        float angleDelta = Mathf.DeltaAngle(previousTouchAngle, currentAngle);

        HandlePinchZoom(distanceDelta);
        HandleTwistRotation(angleDelta);

        previousPinchDistance = currentDistance;
        previousTouchAngle = currentAngle;
    }

    private void HandlePinchZoom(float distanceDelta)
    {
        if (Mathf.Abs(distanceDelta) < 0.01f)
            return;

        float zoomAmount = distanceDelta * zoomStepMultiplier * 0.01f;

        // pinch out = zoom in (lower FOV)
        targetFOV -= zoomAmount;

        targetFOV = Mathf.Clamp(targetFOV, minimumFOV, maximumFOV);
    }

    private void HandleTwistRotation(float angleDelta)
    {
        if (Mathf.Abs(angleDelta) < 0.01f)
            return;

        transform.Rotate(0f, angleDelta * cameraRotationSpeed, 0f, Space.World);
    }
}