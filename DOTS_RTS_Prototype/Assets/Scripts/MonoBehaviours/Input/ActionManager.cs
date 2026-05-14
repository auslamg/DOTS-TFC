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
/// This manager supports box selection, single-click selection, right-click move/attack commands,
/// and formation position generation for multi-unit movement.
/// </remarks>
public class ActionManager : MonoBehaviour
{
    /// <summary>
    /// Mouse position where the current drag-selection started.
    /// </summary>
    private Vector2 selectionStartMousePosition;

    /// <summary>
    /// Current mouse position projected into world space.
    /// </summary>
    private Vector3 mouseWorldPosition => MouseWorldPosition.Instance.GetPosition();

    public GridParametersSO gridParametersSO;


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
    /// Number of units reserved for the center ring.
    /// </summary>
    [SerializeField]
    [Tooltip("Number of units placed in the center group before outer rings are filled.")]
    private int centerUnits = 3;

    /// <summary>
    /// Additional slots added per ring as ring index increases.
    /// </summary>
    [SerializeField]
    [Tooltip("Additional unit slots added for each subsequent ring in circle formation.")]
    private int unitsPerRing = 3;

    /// <summary>
    /// Indicates if the current selection drag started over a UI element.
    /// </summary>
    private bool startedOverUI = false;
    /// <summary>
    /// Entity manager for interacting with ECS entities.
    /// </summary>
    private EntityManager entityManager;
    /// <summary>
    /// Global singleton access to unit selection behavior.
    /// </summary>
    public static ActionManager Instance { get; private set; }

    /// <summary>
    /// Initializes singleton instance state.
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
    /// Initializes the singleton instance.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// Initializes the entity manager.
    /// </summary>
    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    /// <summary>
    /// Handles drag-select and right-click command input each frame.
    /// </summary>
    void Update()
    {
        if (!BuildingPlacementManager.Instance.activeBuildingDataSO.IsNone())
        {
            return;
        }

        if (EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0))
        {
            startedOverUI = true;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            startedOverUI = false;
        }

        if (Input.GetMouseButtonDown(0) && !startedOverUI)
        {
            //Check if the click landed on an entity. The entity is attackable if it has health.
            Entity hitEntity = ClickSphereCastForEntity();
            bool isAttackingAnEntity =
                EntityUtil.ExistsAndPersists(ref entityManager, ref hitEntity) &&
                entityManager.HasComponent<Health>(hitEntity);

            if (isAttackingAnEntity)
            {
                SetTargetOnSelectedUnits(hitEntity);
                Debug.Log($"[Action] Attacking entity {hitEntity.Index}");
            }
            else
            {
                SetDestinationOnSelectedUnits();
                Debug.Log($"[Action] Sending path request: {mouseWorldPosition}");
            }
            SetRallyPositionOffset();
        }
    }

    /// <summary>
    /// Updates rally offsets for selected trainers based on the current mouse position.
    /// </summary>
    private void SetRallyPositionOffset()
    {
        if (!GridUtil.ValidateCoords(
                GridUtil.WorldPositionToCoords(mouseWorldPosition, gridParametersSO.gridCellSize),
                gridParametersSO.size))
        {
            Debug.Log("[Action] Unreachable rally point.");
            return;
        }

        // Query all entities with the Trainer and Selected components to set their rally position offset to the clicked position minus their own position
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).
            WithAll<Selected, Trainer, LocalTransform>().
            Build(entityManager);

        // Register entities and components to modify in order to run Set on the original struct
        NativeArray<Trainer> trainerArray = query.ToComponentDataArray<Trainer>(Allocator.Temp);
        NativeArray<LocalTransform> localTransformArray = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        for (int i = 0; i < trainerArray.Length; i++)
        {
            Trainer trainer = trainerArray[i];
            trainer.rallyPositionOffset = (float3)mouseWorldPosition - localTransformArray[i].Position;
            trainerArray[i] = trainer;
        }
        query.CopyFromComponentDataArray(trainerArray);
        Debug.Log($"[Action] Setting rally point: {mouseWorldPosition}");
    }

    /// <summary>
    /// Sets the target for all TargetOverride Units selected
    /// </summary>
    /// <remarks>
    /// The target will only be set for units of a valid faction (different from the target Unit).
    /// </remarks>
    private void SetTargetOnSelectedUnits(Entity hitEntity)
    {
        //Query all entities with the UnitMover and Selected components to set their target
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).
            WithAll<Selected, Faction>().
            WithPresent<ManualTarget>().
            Build(entityManager);

        //Register entities and components to modify in order to run Set on the original struct
        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        if (entityArray.Length < 1) return; //No entities = no operations to perform
        NativeArray<Faction> factionArray = query.ToComponentDataArray<Faction>(Allocator.Temp);
        NativeArray<ManualTarget> manualTargetArray = query.ToComponentDataArray<ManualTarget>(Allocator.Temp);

        //Get faction for targeted unit
        Faction targetedFaction = entityManager.GetComponentData<Faction>(hitEntity);

        for (int i = 0; i < manualTargetArray.Length; i++)
        {
            //Copy of value, not reference. Setter must use entityManager.SetComponentData()
            ManualTarget newManualTarget = manualTargetArray[i];

            if (factionArray[i].factionID != targetedFaction.factionID)
            {
                newManualTarget.targetEntity = hitEntity;
            }
            manualTargetArray[i] = newManualTarget;
            entityManager.SetComponentEnabled<ManualMove>(entityArray[i], false);
        }
        query.CopyFromComponentDataArray(manualTargetArray); //Remove when implementing single-entity instructions
    }

    /// <summary>
    /// Sets movement destinations for selected units and clears manual targets.
    /// </summary>
    private void SetDestinationOnSelectedUnits()
    {
        Vector3 targetPosition = mouseWorldPosition;
        targetPosition.y = 0f;

        //Query all entities with the UnitMover and Selected components to set their target
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).
            WithAll<Selected>().
            WithPresent<ManualMove, ManualTarget, LocalTransform, PathRequest, FlowFieldRequest, FlowFieldFollower>().
            Build(entityManager);

        //Register entities and components to modify in order to run Set on the original struct
        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        if (entityArray.Length < 1) return; //No entities = no operations to perform
        NativeArray<ManualMove> manualMoveArray = query.ToComponentDataArray<ManualMove>(Allocator.Temp);
        NativeArray<ManualTarget> manualTargetArray = query.ToComponentDataArray<ManualTarget>(Allocator.Temp);
        NativeArray<LocalTransform> localTransformArray = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        NativeArray<PathRequest> pathRequestArray = query.ToComponentDataArray<PathRequest>(Allocator.Temp);

        //Get average position of all entities queried to send it as start position to formation methods
        float3 avgPosition = AveragePositionXZ(localTransformArray);

        //Calculate offset for each selected Unit inside a set formation.
        NativeArray<float3> formationPositionsArray = GenerateFormationPositionsArray(avgPosition, targetPosition, entityArray.Length);

        for (int i = 0; i < manualMoveArray.Length; i++)
        {
            //New ManualMove values
            ManualMove newManualMove = manualMoveArray[i];
            newManualMove.targetPosition = targetPosition;
            newManualMove.postFormationPosition = formationPositionsArray[i];
            manualMoveArray[i] = newManualMove;
            entityManager.SetComponentEnabled<ManualMove>(entityArray[i], true);

            //New ManualTarget values
            ManualTarget newManualTarget = manualTargetArray[i];
            newManualTarget.targetEntity = Entity.Null;
            manualTargetArray[i] = newManualTarget;
            /* entityManager.SetComponentEnabled<ManualTarget>(entityArray[i], true); */

            //New PathRequest values
            PathRequest newPathRequest = pathRequestArray[i];
            newPathRequest.targetPosition = targetPosition;
            newPathRequest.postFormationPosition = formationPositionsArray[i];
            pathRequestArray[i] = newPathRequest;
            // Enable path request to start pathing.
            entityManager.SetComponentEnabled<PathRequest>(entityArray[i], true);

            // Disable FlowField initially, in case it's not necessary.
            entityManager.SetComponentEnabled<FlowFieldRequest>(entityArray[i], false);
            entityManager.SetComponentEnabled<FlowFieldFollower>(entityArray[i], false);
        }
        // Copy to original fields since this is not using reference types but value types
        query.CopyFromComponentDataArray(manualMoveArray);
        query.CopyFromComponentDataArray(manualTargetArray);
        query.CopyFromComponentDataArray(pathRequestArray);
    }

    /// <summary>
    /// Retrieves a clicked-on Entity in the scene (if any) through a SphereCollider cast.
    /// </summary>
    /// <returns>Hit entity when valid; otherwise <see cref="Entity.Null"/>.</returns>
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
    /// Retrieves a clicked-on Entity in the scene (if any) through a Ray cast.
    /// </summary>
    /// <returns>Hit entity when valid; otherwise <see cref="Entity.Null"/>.</returns>
    private Entity ClickRayCastForEntity()
    {
        CollisionWorld collisionWorld = entityManager.GetCollisionWorld();

        //Build raycast from mouse position in appropriate layers
        UnityEngine.Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastInput raycastInput = new RaycastInput
        {
            Start = cameraRay.GetPoint(0f),
            End = cameraRay.GetPoint(5000f), //Arbitrarily large float, but must be kept small-ish for performance cost. Else it would be float.max
            Filter = new CollisionFilter
            {
                BelongsTo = ~0u, //All layers
                CollidesWith = 1u << GameAssets.UNITS_LAYER | 1u << GameAssets.BUILDINGS_LAYER,
                GroupIndex = 0
            }
        };

        //Query Raycast for a single Entity
        if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit raycastHit))
        {
            Entity hitEntity = raycastHit.Entity;
            if (entityManager.Exists(hitEntity) &&
                entityManager.HasComponent<LocalTransform>(hitEntity))
            {
                // CollisionWorld can be one rebuild behind; ignore stale hits.
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
    /// Calculates the average position of all LocalTransform components given.
    /// </summary>
    /// <param name="localTransformArray">Transforms to average.</param>
    /// <returns>Average position projected onto XZ plane.</returns>
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
        avg.y = 0; //Only XZ

        return avg;
    }

    /// <summary>
    /// Calculates individual movement positions for each selected unit in a formation of the requested size.
    /// </summary>
    /// <param name="startPosition">Average start position of selected entities.</param>
    /// <param name="targetPosition">Command target position.</param>
    /// <param name="positionCount">Number of movement slots to generate.</param>
    /// <returns>Array of destination positions for each selected entity.</returns>
    private NativeArray<float3> GenerateFormationPositionsArray(float3 startPosition, float3 targetPosition, int positionCount)
    {
        NativeArray<float3> positionArray = new NativeArray<float3>(positionCount, Allocator.Temp);
        if (positionCount == 0)
        {
            return positionArray;
        }
        positionArray[0] = targetPosition;
        if (positionCount == 1)
        {
            return positionArray;
        }

        /* return CalculateLineFormation(positionArray, startPosition, targetPosition, positionCount); */
        return CalculateCircleFormation(positionArray, targetPosition, positionCount);

    }

    /// <summary>
    /// Calculates the array of individual movement positions in a Line formation.
    /// </summary>
    /// <param name="positionArray">Destination output array.</param>
    /// <param name="startPosition">Average start position of selected entities.</param>
    /// <param name="targetPosition">Command target position.</param>
    /// <param name="positionCount">Number of movement slots to fill.</param>
    /// <returns>Destination array populated with line-formation positions.</returns>
    private NativeArray<float3> CalculateLineFormation(
    NativeArray<float3> positionArray,
    float3 startPosition,
    float3 targetPosition,
    int positionCount)
    {
        float offset = unitOffset;
        float3 targetDirection = targetPosition - startPosition;

        int positionIndex = 0;

        // Calculate angle for proper orientation
        float3 directionNormalized = math.normalize(targetDirection);
        float angle = math.atan2(directionNormalized.x, directionNormalized.z);

        while (positionIndex < positionCount)
        {
            float3 currentTargetVector =
                math.rotate(
                    quaternion.RotateY(angle),
                    new float3(offset * positionIndex, 0, 0));

            float3 centerOffset =
                math.rotate(
                    quaternion.RotateY(angle),
                    new float3(-offset * positionCount / 2, 0, 0));

            // Final position
            float3 currentTargetPosition =
                targetPosition + currentTargetVector + centerOffset;

            positionArray[positionIndex] = currentTargetPosition;

            positionIndex++;
        }

        return positionArray;
    }


    /// <summary>
    /// Calculates the array of individual movement positions in a Circle formation.
    /// </summary>
    /// <param name="positionArray">Destination output array.</param>
    /// <param name="targetPosition">Command target position.</param>
    /// <param name="positionCount">Number of movement slots to fill.</param>
    /// <returns>Destination array populated with circle-formation positions.</returns>
    private NativeArray<float3> CalculateCircleFormation(
    NativeArray<float3> positionArray,
    float3 targetPosition,
    int positionCount)
    {
        float ringRadius = ringOffset;
        int ringIndex = 0;
        int positionIndex = 1;

        while (positionIndex < positionCount)
        {
            int ringPositionCount = centerUnits + ringIndex * unitsPerRing;

            for (int i = 0; i < ringPositionCount && positionIndex < positionCount; i++)
            {
                float angle = i * (math.PI2 / ringPositionCount);

                float3 currentTargetVectorFromCenter =
                    math.rotate(
                        quaternion.RotateY(angle),
                        new float3(ringRadius * (ringIndex + 1), 0, 0));

                float3 currentTargetPosition =
                    targetPosition + currentTargetVectorFromCenter;

                positionArray[positionIndex] = currentTargetPosition;
                positionIndex++;
            }

            ringIndex++;
        }

        return positionArray;
    }
}
