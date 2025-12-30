using UnityEngine;

public class MouseWorldPosition : MonoBehaviour
{
    public static MouseWorldPosition Instance { get; private set; }
    [SerializeField] private bool usePhysics = false;

    /// <summary>
    /// Awake() : MonoBehaviour
    /// Used for singleton logic.
    /// </summary>
    void Awake()
    {
        //Singleton logic
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
    /// Update() : MonoBehaviour
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log(GetPosition());
        }
    }

    /// <summary>
    /// Returns raycasted clicked point of ground.
    /// Relies on usePhysics bool.
    /// </summary>
    public Vector3 GetPosition()
    {
        return usePhysics ? GetPositionFlat() : GetPositionPhysics();
    }

    /// <summary>
    /// Returns raycasted clicked point of ground.
    /// Uses a math plane instead of real physics for optimization and to avoid DOTS-Physics conflicts. Preferable with flat, even terrain.
    /// </summary>
    public Vector3 GetPositionFlat()
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
    /// Returns raycasted clicked point of ground.
    /// Uses physics collisioning raycast to detect complex terrain. Must set a "ground" layerMask.
    /// </summary>
    public Vector3 GetPositionPhysics()
    {
        Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(mouseCameraRay, out RaycastHit hit)) //TODO: Add layerMask
        {
            return hit.point;
        }
        else
        {
            return Vector3.zero;
        }

    }
}
