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
/// Handles RTS-style unit selection and command dispatch for ECS-controlled entities, including movement,
/// targeting, and formation generation.
/// </summary>
/// <remarks>
/// Supports drag selection, click-to-command interactions, sphere/ray entity picking, and formation-based
/// movement distribution for selected units.
/// </remarks>
public class ActionManager : MonoBehaviour
{
    /// <summary>
    /// Mouse screen position where drag selection began.
    /// </summary>
    private Vector2 selectionStartMousePosition;

    /// <summary>
    /// Current world-space mouse position derived from <see cref="MouseWorldPosition"/>.
    /// </summary>
    private Vector3 mouseWorldPosition => MouseWorldPosition.Instance.GetPosition();

    /// <summary>
    /// Grid configuration used for validation and coordinate conversion.
    /// </summary>
    public GridParametersSO gridParametersSO;

    [Header("SphereCast parameters")]

    /// <summary>
    /// Radius used for sphere-based entity selection casts.
    /// </summary>
    [SerializeField]
    [Tooltip("Radius used by single-click sphere cast when selecting entities.")]
    private float sphereCastColliderRadius = 1f;

    [Header("Line formation parameters")]

    /// <summary>
    /// Spacing between units when generating line formations.
    /// </summary>
    [SerializeField]
    [Tooltip("Spacing used between units when line formation is enabled.")]
    private float unitOffset = 1.6f;

    [Header("Ring formation parameters")]

    /// <summary>
    /// Radius increment between successive rings in circle formation.
    /// </summary>
    [SerializeField]
    [Tooltip("Radius increment used between rings in circle formation.")]
    private float ringOffset = 1.6f;

    /// <summary>
    /// Number of units placed in the center ring of a circle formation.
    /// </summary>
    [SerializeField]
    [Tooltip("Number of units placed in the center group before outer rings are filled.")]
    private int centerUnits = 3;

    /// <summary>
    /// Additional unit slots added per ring in circle formation.
    /// </summary>
    [SerializeField]
    [Tooltip("Additional unit slots added for each subsequent ring in circle formation.")]
    private int unitsPerRing = 3;

    /// <summary>
    /// Indicates whether the current drag selection started over a UI element.
    /// </summary>
    private bool startedOverUI = false;

    /// <summary>
    /// ECS entity manager used for querying and modifying entities.
    /// </summary>
    private EntityManager entityManager;

    /// <summary>
    /// Global singleton instance of the ActionManager.
    /// </summary>
    public static ActionManager Instance { get; private set; }

    /// <summary>
    /// Ensures singleton instance integrity.
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
    /// Unity lifecycle method. Initializes singleton instance.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// Unity lifecycle method. Initializes ECS entity manager reference.
    /// </summary>
    private void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    /// <summary>
    /// Handles per-frame input for selection, movement commands, and attack targeting.
    /// </summary>
    private void Update()
    {
        if (GameModeManager.Instance.activeGameMode == GameMode.BuildMode)
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
    /// Updates rally position offsets for selected trainer units based on mouse world position.
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

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Selected, Trainer, LocalTransform>()
            .Build(entityManager);

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
    /// Assigns an attack target to all selected units when a valid enemy entity is clicked.
    /// </summary>
    /// <param name="hitEntity">Target entity to attack.</param>
    /// <remarks>
    /// Only assigns targets to entities with mismatched faction IDs.
    /// </remarks>
    private void SetTargetOnSelectedUnits(Entity hitEntity)
    {
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Selected, Faction>()
            .WithPresent<ManualTarget>()
            .Build(entityManager);

        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        if (entityArray.Length < 1) return;

        NativeArray<Faction> factionArray = query.ToComponentDataArray<Faction>(Allocator.Temp);
        NativeArray<ManualTarget> manualTargetArray = query.ToComponentDataArray<ManualTarget>(Allocator.Temp);

        Faction targetedFaction = entityManager.GetComponentData<Faction>(hitEntity);

        for (int i = 0; i < manualTargetArray.Length; i++)
        {
            ManualTarget newManualTarget = manualTargetArray[i];

            if (factionArray[i].factionID != targetedFaction.factionID)
            {
                newManualTarget.targetEntity = hitEntity;
            }

            manualTargetArray[i] = newManualTarget;
            entityManager.SetComponentEnabled<ManualMove>(entityArray[i], false);
        }

        query.CopyFromComponentDataArray(manualTargetArray);
    }

    /// <summary>
    /// Issues movement commands and formation destinations to all selected units.
    /// </summary>
    private void SetDestinationOnSelectedUnits()
    {
        Vector3 targetPosition = mouseWorldPosition;
        targetPosition.y = 0f;

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Selected>()
            .WithPresent<ManualMove, ManualTarget, LocalTransform, PathRequest, FlowFieldRequest, FlowFieldFollower>()
            .Build(entityManager);

        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        if (entityArray.Length < 1) return;

        NativeArray<ManualMove> manualMoveArray = query.ToComponentDataArray<ManualMove>(Allocator.Temp);
        NativeArray<ManualTarget> manualTargetArray = query.ToComponentDataArray<ManualTarget>(Allocator.Temp);
        NativeArray<LocalTransform> localTransformArray = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        NativeArray<PathRequest> pathRequestArray = query.ToComponentDataArray<PathRequest>(Allocator.Temp);

        float3 avgPosition = AveragePositionXZ(localTransformArray);
        NativeArray<float3> formationPositionsArray =
            GenerateFormationPositionsArray(avgPosition, targetPosition, entityArray.Length);

        for (int i = 0; i < manualMoveArray.Length; i++)
        {
            ManualMove newManualMove = manualMoveArray[i];
            newManualMove.targetPosition = targetPosition;
            newManualMove.postFormationPosition = formationPositionsArray[i];
            manualMoveArray[i] = newManualMove;
            entityManager.SetComponentEnabled<ManualMove>(entityArray[i], true);

            ManualTarget newManualTarget = manualTargetArray[i];
            newManualTarget.targetEntity = Entity.Null;
            manualTargetArray[i] = newManualTarget;

            PathRequest newPathRequest = pathRequestArray[i];
            newPathRequest.targetPosition = targetPosition;
            newPathRequest.postFormationPosition = formationPositionsArray[i];
            pathRequestArray[i] = newPathRequest;

            entityManager.SetComponentEnabled<PathRequest>(entityArray[i], true);
            entityManager.SetComponentEnabled<FlowFieldRequest>(entityArray[i], false);
            entityManager.SetComponentEnabled<FlowFieldFollower>(entityArray[i], false);
        }

        query.CopyFromComponentDataArray(manualMoveArray);
        query.CopyFromComponentDataArray(manualTargetArray);
        query.CopyFromComponentDataArray(pathRequestArray);
    }

    /// <summary>
    /// Performs a sphere cast from the mouse position to detect a valid ECS entity under the cursor.
    /// </summary>
    /// <returns>Hit entity if valid; otherwise <see cref="Entity.Null"/>.</returns>
    private unsafe Entity ClickSphereCastForEntity()
    {
        CollisionWorld collisionWorld = entityManager.GetCollisionWorld();

        UnityEngine.Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        float3 start = cameraRay.GetPoint(0f);
        float3 end = cameraRay.GetPoint(5000f);

        SphereGeometry sphereGeometry = new SphereGeometry
        {
            Center = float3.zero,
            Radius = sphereCastColliderRadius
        };

        CollisionFilter filter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = 1u << GameAssets.UNITS_LAYER | 1u << GameAssets.BUILDINGS_LAYER,
            GroupIndex = 0
        };

        using (BlobAssetReference<Collider> sphereCollider =
               SphereCollider.Create(sphereGeometry, filter))
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
    /// Performs a raycast from the mouse position to detect a valid ECS entity.
    /// </summary>
    /// <returns>Hit entity if valid; otherwise <see cref="Entity.Null"/>.</returns>
    private Entity ClickRayCastForEntity()
    {
        CollisionWorld collisionWorld = entityManager.GetCollisionWorld();

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

        if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit raycastHit))
        {
            Entity hitEntity = raycastHit.Entity;

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

        return Entity.Null;
    }

    /// <summary>
    /// Computes the average XZ-plane position of a set of transforms.
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
        avg.y = 0;

        return avg;
    }

    /// <summary>
    /// Generates formation positions for a group of units based on a target destination.
    /// </summary>
    /// <param name="startPosition">Average starting position of selected units.</param>
    /// <param name="targetPosition">Destination position.</param>
    /// <param name="positionCount">Number of units to position.</param>
    /// <returns>Array of formation positions.</returns>
    private NativeArray<float3> GenerateFormationPositionsArray(
        float3 startPosition,
        float3 targetPosition,
        int positionCount)
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

        return CalculateCircleFormation(positionArray, targetPosition, positionCount);
    }

    /// <summary>
    /// Generates a line formation layout for selected units.
    /// </summary>
    /// <param name="positionArray">Output array to populate.</param>
    /// <param name="startPosition">Formation origin reference.</param>
    /// <param name="targetPosition">Target destination.</param>
    /// <param name="positionCount">Number of units.</param>
    /// <returns>Populated formation array.</returns>
    private NativeArray<float3> CalculateLineFormation(
        NativeArray<float3> positionArray,
        float3 startPosition,
        float3 targetPosition,
        int positionCount)
    {
        float offset = unitOffset;
        float3 targetDirection = targetPosition - startPosition;

        int positionIndex = 0;

        float3 directionNormalized = math.normalize(targetDirection);
        float angle = math.atan2(directionNormalized.x, directionNormalized.z);

        while (positionIndex < positionCount)
        {
            float3 currentTargetVector =
                math.rotate(quaternion.RotateY(angle),
                    new float3(offset * positionIndex, 0, 0));

            float3 centerOffset =
                math.rotate(quaternion.RotateY(angle),
                    new float3(-offset * positionCount / 2, 0, 0));

            float3 currentTargetPosition =
                targetPosition + currentTargetVector + centerOffset;

            positionArray[positionIndex] = currentTargetPosition;
            positionIndex++;
        }

        return positionArray;
    }

    /// <summary>
    /// Generates a circular formation layout for selected units.
    /// </summary>
    /// <param name="positionArray">Output array to populate.</param>
    /// <param name="targetPosition">Center position of formation.</param>
    /// <param name="positionCount">Number of units.</param>
    /// <returns>Populated formation array.</returns>
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
                    math.rotate(quaternion.RotateY(angle),
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