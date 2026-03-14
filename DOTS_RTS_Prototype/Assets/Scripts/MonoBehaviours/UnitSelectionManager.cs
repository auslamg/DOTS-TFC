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

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance { get; private set; }

    public event EventHandler OnSelectionAreaStart;
    public event EventHandler OnSelectionAreaEnd;
    public event EventHandler OnSelectionChange;


    private Vector2 selectionStartMousePosition;

    private Vector3 mouseWorldPosition => MouseWorldPosition.Instance.GetPosition();


    [Header("SphereCast parameters")]
    [SerializeField] private float sphereCastColliderRadius = 1.5f;


    [Header("Line formation parameters")]
    [SerializeField] private float unitOffset = 1.6f;

    [Header("Ring formation parameters")]
    [SerializeField] private float ringOffset = 1.6f;
    [SerializeField] private int centerUnits = 3;
    [SerializeField] private int unitsPerRing = 3;



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
    /// //TODO: Document extensively
    /// </summary>
    void Update()
    {
        //TODO: This disables selection if ANY GameObject is in front of the mouse. Refactor to only disable if pointer is over UI.
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            selectionStartMousePosition = Input.mousePosition;
            OnSelectionAreaStart?.Invoke(this, EventArgs.Empty);
        }
        if (Input.GetMouseButtonUp(0))
        {
            Vector2 selectionEndMousePosition = Input.mousePosition;

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            //TODO: Extract into DeselectAll() method
            //Query all entities with the Selected component to disable it
            EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<Selected>().Build(entityManager);

            NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
            NativeArray<Selected> selectedArray = query.ToComponentDataArray<Selected>(Allocator.Temp);
            for (int i = 0; i < entityArray.Length; i++)
            {
                entityManager.SetComponentEnabled<Selected>(entityArray[i], false);
                Selected selected = selectedArray[i];
                selected.onDeselected = true;
                entityManager.SetComponentData(entityArray[i], selected);
            }



            Rect selectionAreaRect = GetSelectionAreaRect();
            float selectionAreaSize = selectionAreaRect.width + selectionAreaRect.height;
            float multipleSelectionSizeMinimum = 40f;
            bool isMultipleSelection = selectionAreaSize >= multipleSelectionSizeMinimum;

            //TODO: Extract into SelectArea() method
            if (isMultipleSelection)
            {
                //Query all entities with the UnitMover and Selected components to check if they're inside the SelectionAreaRect
                query = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform>().WithPresent<Selected>().Build(entityManager);

                //Register entities and components to access LocalTransform component and Entity memory adress
                entityArray = query.ToEntityArray(Allocator.Temp);
                NativeArray<LocalTransform> localTransformArray = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
                for (int i = 0; i < localTransformArray.Length; i++)
                {
                    //Convert unit LocalTransform position into screen position to check if it is within the SelectionAreaRect
                    LocalTransform unitLocalTransform = localTransformArray[i];
                    Vector2 entityScreenPosition = Camera.main.WorldToScreenPoint(unitLocalTransform.Position);

                    //Unit inside of selection area
                    if (selectionAreaRect.Contains(entityScreenPosition))
                    {
                        entityManager.SetComponentEnabled<Selected>(entityArray[i], true);
                        Selected selected = entityManager.GetComponentData<Selected>(entityArray[i]);
                        selected.onSelected = true;
                        entityManager.SetComponentData(entityArray[i], selected);
                    }
                }
            }
            //TODO: Extract into SelectSingle() method
            else
            {
                Entity hitEntity = ClickSphereCastForEntity(entityManager);
                if (EntityUtil.ExistsAndPersists(ref entityManager, ref hitEntity))
                {
                    //An entity was hit
                    if (entityManager.HasComponent<Faction>(hitEntity) && entityManager.HasComponent<Selected>(hitEntity))
                    {
                        //A Faction entity was hit > Select unit
                        entityManager.SetComponentEnabled<Selected>(hitEntity, true);
                        Selected selected = entityManager.GetComponentData<Selected>(hitEntity);
                        selected.onSelected = true;
                        entityManager.SetComponentData(hitEntity, selected);
                    }
                }
            }

            OnSelectionAreaEnd?.Invoke(this, EventArgs.Empty);
            OnSelectionChange?.Invoke(this, EventArgs.Empty);
        }
        //TODO: Extract to ControlsManager.cs
        if (Input.GetMouseButtonDown(1))
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            //Check if the click landed on an entity
            Entity hitEntity = ClickRayCastForEntity(entityManager);
            bool isAttackingAnEntity =
                EntityUtil.ExistsAndPersists(ref entityManager, ref hitEntity) &&
                entityManager.HasComponent<Health>(hitEntity);
            //NOTE: Health is used a common ground for attackable units and buildings

            if (isAttackingAnEntity)
            {
                SetTargetOnSelectedUnits(entityManager, hitEntity);
            }
            else
            {
                SetDestinationOnSelectedUnits(entityManager);
            }
            SetRallyPositionOffset(entityManager);
        }
    }

    private void SetRallyPositionOffset(EntityManager entityManager)
    {
        // Query all entities with the Trainer and Selected components to set their rally position offset to the clicked position minus their own position
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<
            Selected,
            Trainer,
            LocalTransform>().Build(entityManager);

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
    }

    /// <summary>
    /// Sets the target for all TargetOverride Units selected
    /// </summary>
    /// <remarks>
    /// The target will only be set for units of a valid faction (different from the target Unit).
    /// </remarks>
    private void SetTargetOnSelectedUnits(EntityManager entityManager, Entity hitEntity)
    {
        //Query all entities with the UnitMover and Selected components to set their target
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<
            Selected,
            Faction>().
            WithPresent<ManualTarget>().Build(entityManager);

        //Register entities and components to modify in order to run Set on the original struct
        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        if (entityArray.Length < 1) return; //No entities = no operations to perform
        NativeArray<Faction> factionArray = query.ToComponentDataArray<Faction>(Allocator.Temp);
        NativeArray<ManualTarget> manualTargetArray = query.ToComponentDataArray<ManualTarget>(Allocator.Temp);

        //Get faction for targetted unit
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
    /// Sets the movement destination for all UnitMover/MoveOverride Units selected
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    private void SetDestinationOnSelectedUnits(EntityManager entityManager)
    {
        Vector3 targetPosition = mouseWorldPosition;
        targetPosition.y = 0f;

        //Query all entities with the UnitMover and Selected components to set their target
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<
            UnitMover,
            Selected,
            LocalTransform>().
            WithPresent<ManualMove, ManualTarget>().Build(entityManager);

        //Register entities and components to modify in order to run Set on the original struct
        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        if (entityArray.Length < 1) return; //No entities = no operations to perform
        NativeArray<ManualMove> manualMoveArray = query.ToComponentDataArray<ManualMove>(Allocator.Temp);
        NativeArray<ManualTarget> manualTargetArray = query.ToComponentDataArray<ManualTarget>(Allocator.Temp);
        NativeArray<LocalTransform> localTransformArray = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        //Get average position of all entities queried to send it as start position to formation methods
        float3 avgPosition = float3.zero;
        avgPosition = AveragePositionXZ(localTransformArray);

        //Calculate offset for each selected Unit inside a set formation.
        NativeArray<float3> formationPositionsArray = GenerateFormationPositionsArray(avgPosition, targetPosition, entityArray.Length);

        for (int i = 0; i < manualMoveArray.Length; i++)
        {
            //New MoveOverride values
            ManualMove newManualMove = manualMoveArray[i];
            newManualMove.targetPosition = formationPositionsArray[i];
            manualMoveArray[i] = newManualMove;
            entityManager.SetComponentEnabled<ManualMove>(entityArray[i], true);

            //New TargetOverride values
            ManualTarget newManualTarget = manualTargetArray[i];
            newManualTarget.targetEntity = Entity.Null;
            manualTargetArray[i] = newManualTarget;
            entityManager.SetComponentEnabled<ManualMove>(entityArray[i], true);

            //[Deprecated]
            // Single-entity instruction alternative.
            /* entityManager.SetComponentData(entityArray[i], newUnitMover);  */

            // Writing a new local array and copying values to the query is preferable since it reduces writing operations.

        }
        query.CopyFromComponentDataArray(manualMoveArray); //Remove when implementing single-entity instructions
        query.CopyFromComponentDataArray(manualTargetArray); //Remove when implementing single-entity instructions
    }

    /// <summary>
    /// Retrieves a clicked-on Entity in the scene (if any) through a SphereCollider cast.
    /// </summary>
    private unsafe Entity ClickSphereCastForEntity(EntityManager entityManager)
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

        BlobAssetReference<Collider> sphereCollider = SphereCollider.Create(sphereGeometry, filter);

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

                sphereCollider.Dispose();
                return hitEntity;
            }
        }

        sphereCollider.Dispose();
        return Entity.Null;
    }



    /// <summary>
    /// Retrieves a clicked-on Entity in the scene (if any) through a Ray cast.
    /// </summary>
    private Entity ClickRayCastForEntity(EntityManager entityManager)
    {
        CollisionWorld collisionWorld = entityManager.GetCollisionWorld();

        //Build raycast from mouse position in appropiate layers
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

    //TODO: Extract
    /// <summary>
    /// Calculates the average position of all LocalTransform components given.
    /// </summary>
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
    /// Calculates the box-select feature's SelectionAreaRectangle.
    /// </summary>
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
    /// Calculates the array of individual movement positions for each UnitMober component of a given size.
    ///TODO: Implement additional formations like Line, Square and Wedge.
    /// </summary>
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

        //TODO: Implement formations.
        /* return CalculateLineFormation(positionArray, startPosition, targetPosition, positionCount); */
        return CalculateCircleFormation(positionArray, targetPosition, positionCount);

    }

    /// <summary>
    /// Calculates the array of individual movement positions in a Line formation.
    /// </summary>
    private NativeArray<float3> CalculateLineFormation(NativeArray<float3> positionArray, float3 startPosition, float3 targetPosition, int positionCount)
    {
        float offest = unitOffset;
        float3 targetDirection = targetPosition - startPosition;
        int positionIndex = 0;

        //Calculate angle for proper orientation
        float3 directionNormalized = math.normalize(targetDirection);
        float angle = math.atan2(directionNormalized.x, directionNormalized.z);

        while (positionIndex < positionCount)
        {
            //Used for offsetting center
            bool isEvenCount = positionIndex % 2 == 0;
            //Decide right or left
            bool isRight = positionIndex % 2 == 0;

            float3 currentTargetVector = math.rotate(quaternion.RotateY(angle), new float3(unitOffset * positionIndex, 0, 0));
            float3 centerOffset = math.rotate(quaternion.RotateY(angle), new float3(-unitOffset * positionCount / 2, 0, 0));


            //Posicion final
            float3 currentTargetPosition = targetPosition + currentTargetVector + centerOffset;

            positionArray[positionIndex] = currentTargetPosition;
            positionIndex++;

            //FIX: Refactor into no break use
            if (positionIndex >= positionCount)
            {
                break;
            }
        }
        return positionArray;
    }


    /// <summary>
    /// Calculates the array of individual movement positions in a Cricle formation.
    /// </summary>
    private NativeArray<float3> CalculateCircleFormation(NativeArray<float3> positionArray, float3 targetPosition, int positionCount)
    {
        float ringRadius = ringOffset;
        int ringIndex = 0;
        int positionIndex = 1;

        while (positionIndex < positionCount)
        {
            int ringPositionCount = centerUnits + ringIndex * unitsPerRing;

            for (int i = 0; i < ringPositionCount; i++)
            {
                float angle = i * (math.PI2 / ringPositionCount);
                float3 currentTargetVectorFromCenter = math.rotate(quaternion.RotateY(angle), new float3(ringRadius * (ringIndex + 1), 0, 0));
                float3 currentTargetPosition = targetPosition + currentTargetVectorFromCenter;

                positionArray[positionIndex] = currentTargetPosition;
                positionIndex++;

                //FIX: Refactor into no break use
                if (positionIndex >= positionCount)
                {
                    break;
                }

            }
            ringIndex++;
        }
        return positionArray;
    }



}
