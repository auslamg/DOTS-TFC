using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.EventSystems;
using Collider = Unity.Physics.Collider;
using SphereCollider = Unity.Physics.SphereCollider;

/// <summary>
/// Handles RTS-style unit selection and command dispatch (move/attack/rally) for selected entities.
/// </summary>
/// <remarks>
/// This manager supports box selection, single-click selection, right-click commands,
/// and formation position generation for multi-unit movement.
/// </remarks>
public class SelectionManager : MonoBehaviour
{
    /// <summary>
    /// Mouse position where the current drag-selection started.
    /// </summary>
    private Vector2 selectionStartMousePosition;

    /// <summary>
    /// Current mouse position projected into world space.
    /// </summary>
    private Vector3 mouseWorldPosition => MouseWorldPosition.Instance.GetPosition();

    [Header("SphereCast parameters")]

    /// <summary>
    /// Radius used by single-click sphere cast selection.
    /// </summary>
    [SerializeField]
    [Tooltip("Radius used by single-click sphere cast when selecting entities.")]
    private float sphereCastColliderRadius = 1f;

    [Header("Line formation parameters")]

    /// <summary>
    /// Horizontal spacing used by line-formation calculations.
    /// </summary>
    [SerializeField]
    [Tooltip("Spacing used between units when line formation is enabled.")]
    private float unitOffset = 1.6f;

    [Header("Ring formation parameters")]

    /// <summary>
    /// Radius step used between rings in circle formation.
    /// </summary>
    [SerializeField]
    [Tooltip("Radius increment used between rings in circle formation.")]
    private float ringOffset = 1.6f;

    /// <summary>
    /// Number of units placed in the center ring before outer rings are generated.
    /// </summary>
    [SerializeField]
    [Tooltip("Number of units placed in the center group before outer rings are filled.")]
    private int centerUnits = 3;

    /// <summary>
    /// Additional unit slots added per ring as ring index increases.
    /// </summary>
    [SerializeField]
    [Tooltip("Additional unit slots added for each subsequent ring in circle formation.")]
    private int unitsPerRing = 3;

    /// <summary>
    /// Raised when drag-selection starts.
    /// </summary>
    public event EventHandler OnSelectionAreaStart;

    /// <summary>
    /// Raised when drag-selection ends.
    /// </summary>
    public event EventHandler OnSelectionAreaEnd;

    /// <summary>
    /// Raised whenever the selected entity set changes.
    /// </summary>
    public event EventHandler OnSelectionChange;

    /// <summary>
    /// Indicates whether the current selection drag started over a UI element.
    /// </summary>
    private bool startedOverUI = false;

    /// <summary>
    /// Cached ECS EntityManager used to interact with entities.
    /// </summary>
    private EntityManager entityManager;

    /// <summary>
    /// Global singleton instance for selection management.
    /// </summary>
    public static SelectionManager Instance { get; private set; }

    /// <summary>
    /// Initializes singleton instance state and enforces single instance rule.
    /// </summary>
    private void InitializeSingleton()
    {
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
    /// Unity Awake callback. Initializes singleton instance.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// Unity Start callback. Subscribes to events and initializes ECS entity manager.
    /// </summary>
    void Start()
    {
        DOTSEventManager.Instance.OnSelectedDeath += DOTSEventManager_OnSelectedDeath;
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    /// <summary>
    /// Handles event when a selected unit dies and triggers selection update.
    /// </summary>
    /// <param name="sender">Event source.</param>
    /// <param name="e">Event arguments.</param>
    private void DOTSEventManager_OnSelectedDeath(object sender, EventArgs e)
    {
        TriggerOnSelectionChange();
    }

    /// <summary>
    /// Processes input each frame for drag selection and single-click selection.
    /// </summary>
    void Update()
    {
        if (!BuildingPlacementManager.Instance.activeBuildingDataSO.IsNone())
        {
            return;
        }

        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButtonDown(0))
            {
                startedOverUI = false;
                selectionStartMousePosition = Input.mousePosition;
                OnSelectionAreaStart?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                startedOverUI = true;
            }
        }

        if (Input.GetMouseButtonUp(0) & !startedOverUI)
        {
            Vector2 selectionEndMousePosition = Input.mousePosition;

            // Deselect all currently selected entities before applying new selection.
            DeselectAll();

            // Distinguish between single select and rectangle select.
            Rect selectionAreaRect = GetSelectionAreaRect();
            float selectionAreaSize = selectionAreaRect.width + selectionAreaRect.height;
            float multipleSelectionSizeMinimum = 40f;
            bool isMultipleSelection = selectionAreaSize >= multipleSelectionSizeMinimum;

            if (isMultipleSelection)
            {
                SelectInArea(selectionAreaRect);
            }
            else
            {
                SelectSingle();
            }

            OnSelectionAreaEnd?.Invoke(this, EventArgs.Empty);
            OnSelectionChange?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Deselects all currently selected entities in the world.
    /// </summary>
    public void DeselectAll()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EntityQuery selectableQuery =
            new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Selected>()
            .Build(entityManager);

        NativeArray<Entity> entityArray = selectableQuery.ToEntityArray(Allocator.Temp);
        NativeArray<Selected> selectedArray = selectableQuery.ToComponentDataArray<Selected>(Allocator.Temp);

        for (int i = 0; i < entityArray.Length; i++)
        {
            entityManager.SetComponentEnabled<Selected>(entityArray[i], false);
            Selected selected = selectedArray[i];
            selected.onDeselected = true;
            entityManager.SetComponentData(entityArray[i], selected);
        }
    }

    /// <summary>
    /// Performs single-entity selection using a sphere cast under the cursor.
    /// </summary>
    private void SelectSingle()
    {
        Entity hitEntity = ClickSphereCastForEntity();

        if (EntityUtil.ExistsAndPersists(ref entityManager, ref hitEntity))
        {
            // An entity was hit.
            if (entityManager.HasComponent<Faction>(hitEntity) && entityManager.HasComponent<Selected>(hitEntity))
            {
                // A valid selectable faction entity was hit.
                entityManager.SetComponentEnabled<Selected>(hitEntity, true);
                Selected selected = entityManager.GetComponentData<Selected>(hitEntity);
                selected.onSelected = true;
                entityManager.SetComponentData(hitEntity, selected);
            }
        }
    }

    /// <summary>
    /// Selects multiple entities inside a drag-selection rectangle.
    /// Applies priority rules: Units > Buildings > None.
    /// </summary>
    private void SelectInArea(Rect selectionAreaRect)
    {
        // Query all entities with the LocalTransform and Selected components.
        EntityQuery query =
            new EntityQueryBuilder(Allocator.Temp)
            .WithAll<LocalTransform>()
            .WithPresent<Selected>()
            .Build(entityManager);

        // Register entities and components to access LocalTransform and entity references.
        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        NativeArray<LocalTransform> localTransformArray = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        NativeList<Entity> units = new NativeList<Entity>(Allocator.Temp);
        NativeList<Entity> buildings = new NativeList<Entity>(Allocator.Temp);

        Camera cam = Camera.main;

        for (int i = 0; i < localTransformArray.Length; i++)
        {
            // Convert world position into screen position to check if inside selection rectangle.
            LocalTransform unitLocalTransform = localTransformArray[i];
            Vector2 entityScreenPosition = cam.WorldToScreenPoint(unitLocalTransform.Position);

            // Entity is inside selection area.
            if (selectionAreaRect.Contains(entityScreenPosition))
            {
                Entity e = entityArray[i];

                // Priority classification: separate units and buildings.
                if (entityManager.HasComponent<Unit>(e))
                {
                    units.Add(e);
                }
                else if (entityManager.HasComponent<Building>(e))
                {
                    buildings.Add(e);
                }
            }
        }

        // Selection priority filtering.
        NativeArray<Entity> finalSelection;

        // If any units exist, select ONLY units.
        if (units.Length > 0)
        {
            finalSelection = units.AsArray();
        }
        // If no units exist but buildings exist, select ONLY buildings.
        else if (buildings.Length > 0)
        {
            finalSelection = buildings.AsArray();
        }
        else
        {
            units.Dispose();
            buildings.Dispose();
            return;
        }

        for (int i = 0; i < finalSelection.Length; i++)
        {
            Entity e = finalSelection[i];

            entityManager.SetComponentEnabled<Selected>(e, true);
            Selected selected = entityManager.GetComponentData<Selected>(e);
            selected.onSelected = true;
            entityManager.SetComponentData(e, selected);
        }

        units.Dispose();
        buildings.Dispose();
    }

    /// <summary>
    /// Performs a sphere-cast under the mouse cursor and returns the hit entity.
    /// </summary>
    /// <returns>Hit entity if valid; otherwise <see cref="Entity.Null"/>.</returns>
    private unsafe Entity ClickSphereCastForEntity()
    {
        CollisionWorld collisionWorld = entityManager.GetCollisionWorld();

        UnityEngine.Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        float3 start = cameraRay.GetPoint(0f);
        float3 end = cameraRay.GetPoint(5000f);

        float radius = sphereCastColliderRadius;

        SphereGeometry sphereGeometry = new SphereGeometry
        {
            Center = float3.zero,
            Radius = radius
        };

        CollisionFilter filter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = 1u << GameAssets.UNITS_LAYER | 1u << GameAssets.BUILDINGS_LAYER,
            GroupIndex = 0
        };

        using (BlobAssetReference<Collider> sphereCollider = SphereCollider.Create(sphereGeometry, filter))
        {
            ColliderCastInput input = new ColliderCastInput
            {
                Collider = (Collider*)sphereCollider.GetUnsafePtr(),
                Orientation = quaternion.identity,
                Start = start,
                End = end
            };

            if (collisionWorld.CastCollider(input, out ColliderCastHit hit))
            {
                Entity hitEntity = hit.Entity;

                if (entityManager.Exists(hitEntity) &&
                    entityManager.HasComponent<LocalTransform>(hitEntity))
                {
                    if (!entityManager.HasComponent<PhysicsCollider>(hitEntity))
                        return Entity.Null;

                    if (entityManager.HasComponent<Health>(hitEntity))
                    {
                        Health hitHealth = entityManager.GetComponentData<Health>(hitEntity);
                        if (hitHealth.currentHealth <= 0)
                            return Entity.Null;
                    }

                    return hitEntity;
                }
            }
        }

        return Entity.Null;
    }

    /// <summary>
    /// Performs a raycast under the mouse cursor and returns the hit entity.
    /// </summary>
    /// <returns>Hit entity if valid; otherwise <see cref="Entity.Null"/>.</returns>
    private Entity ClickRayCastForEntity()
    {
        CollisionWorld collisionWorld = entityManager.GetCollisionWorld();

        // Build raycast from mouse position using physics collision layers.
        UnityEngine.Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastInput raycastInput = new RaycastInput
        {
            Start = cameraRay.GetPoint(0f),
            End = cameraRay.GetPoint(5000f),
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.UNITS_LAYER | 1u << GameAssets.BUILDINGS_LAYER,
                GroupIndex = 0
            }
        };

        // Query physics world for raycast hit.
        if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit raycastHit))
        {
            Entity hitEntity = raycastHit.Entity;

            if (entityManager.Exists(hitEntity) &&
                entityManager.HasComponent<LocalTransform>(hitEntity))
            {
                // Physics world may be slightly out of sync; ignore invalid hits.
                if (!entityManager.HasComponent<PhysicsCollider>(hitEntity))
                {
                    return Entity.Null;
                }

                if (entityManager.HasComponent<Health>(hitEntity))
                {
                    Health hitHealth = entityManager.GetComponentData<Health>(hitEntity);
                    if (hitHealth.currentHealth <= 0)
                    {
                        return Entity.Null;
                    }
                }

                return hitEntity;
            }
        }

        return Entity.Null;
    }

    /// <summary>
    /// Calculates the average world position of a set of LocalTransform components (XZ plane only).
    /// </summary>
    /// <param name="localTransformArray">Array of transforms to average.</param>
    /// <returns>The average position projected onto the XZ plane.</returns>
    private static float3 AveragePositionXZ(NativeArray<LocalTransform> localTransformArray)
    {
        if (localTransformArray.Length == 0)
            throw new InvalidOperationException("Cannot calculate average of zero elements");

        float3 sum = float3.zero;

        for (int i = 0; i < localTransformArray.Length; i++)
        {
            sum += localTransformArray[i].Position;
        }

        float3 avg = sum / localTransformArray.Length;
        avg.y = 0;

        return avg;
    }

    /// <summary>
    /// Computes the rectangle used for drag-selection in screen space.
    /// </summary>
    /// <returns>The screen-space selection rectangle.</returns>
    public Rect GetSelectionAreaRect()
    {
        Vector2 selectionEndMousePosition = Input.mousePosition;

        Vector2 lowerLeftCorner = new Vector2(
            Mathf.Min(selectionStartMousePosition.x, selectionEndMousePosition.x),
            Mathf.Min(selectionStartMousePosition.y, selectionEndMousePosition.y)
        );

        Vector2 upperRightCorner = new Vector2(
            Mathf.Max(selectionStartMousePosition.x, selectionEndMousePosition.x),
            Mathf.Max(selectionStartMousePosition.y, selectionEndMousePosition.y)
        );

        return new Rect(
            lowerLeftCorner.x,
            lowerLeftCorner.y,
            width: upperRightCorner.x - lowerLeftCorner.x,
            height: upperRightCorner.y - lowerLeftCorner.y
        );
    }

    /// <summary>
    /// Triggers the selection change event externally.
    /// </summary>
    public void TriggerOnSelectionChange()
    {
        Debug.Log("Triggered OnSelectionChange from outside");
        OnSelectionChange?.Invoke(this, EventArgs.Empty);
    }
}