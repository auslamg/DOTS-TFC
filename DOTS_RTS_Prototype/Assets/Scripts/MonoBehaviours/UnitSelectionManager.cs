using System;
using Unity.Android.Gradle.Manifest;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance { get; private set; }

    public event EventHandler OnSelectionAreaStart;
    public event EventHandler OnSelectionAreaEnd;

    private Vector2 selectionStartMousePosition;

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

            //TODO: Extract into SelectSingle() method

            Rect selectionAreaRect = GetSelectionAreaRect();
            float selectionAreaSize = selectionAreaRect.width + selectionAreaRect.height;
            float multipleSelectionSizeMinimum = 40f;
            bool isMultipleSelection = selectionAreaSize >= multipleSelectionSizeMinimum;

            if (isMultipleSelection)
            {
                //TODO: Extract into SelectArea() method
                //Query all entities with the UnitMover and Selected components to check if they're inside the SelectionAreaRect
                query = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, Unit>().WithPresent<Selected>().Build(entityManager);


                //Register entities and components to access LocalTransform component and Entity memory adress
                entityArray = query.ToEntityArray(Allocator.Temp);
                NativeArray<LocalTransform> localTransformArray = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
                for (int i = 0; i < localTransformArray.Length; i++)
                {
                    //Convert unit LocalTransform position into screen position to check if it is within the SelectionAreaRect
                    LocalTransform unitLocalTransform = localTransformArray[i];
                    Vector2 unitScreenPosition = Camera.main.WorldToScreenPoint(unitLocalTransform.Position);

                    //Unit inside of selection area
                    if (selectionAreaRect.Contains(unitScreenPosition))
                    {
                        entityManager.SetComponentEnabled<Selected>(entityArray[i], true);
                        Selected selected = entityManager.GetComponentData<Selected>(entityArray[i]);
                        selected.onSelected = true;
                        entityManager.SetComponentData(entityArray[i], selected);
                    }
                }
            }
            else
            {
                Entity hitEntity = ClickRayCastForEntity(entityManager);

                if (entityManager.ExistsAndPersists(hitEntity))
                {
                    //An entity was hit
                    if (entityManager.HasComponent<Unit>(hitEntity) && entityManager.HasComponent<Selected>(hitEntity))
                    {
                        //A Unit was hit > Select unit

                        entityManager.SetComponentEnabled<Selected>(hitEntity, true);
                        Selected selected = entityManager.GetComponentData<Selected>(hitEntity);
                        selected.onSelected = true;
                        entityManager.SetComponentData(hitEntity, selected);
                    }
                }
            }

            OnSelectionAreaEnd?.Invoke(this, EventArgs.Empty);
        }
        if (Input.GetMouseButtonDown(1))
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            //Check if the click landed on an entity
            Entity hitEntity = ClickRayCastForEntity(entityManager);
            bool isAttackingAnEntity =
                entityManager.ExistsAndPersists(hitEntity) &&
                entityManager.HasComponent<Unit>(hitEntity);

            if (isAttackingAnEntity)
            {
                SetTargetOnSelectedUnits(entityManager, hitEntity);
            }
            else
            {
                SetDestinationOnSelectedUnits(entityManager);
            }
        }
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
            WithPresent<TargetOverride>().Build(entityManager);

        //Register entities and components to modify in order to run Set on the original struct
        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        if (entityArray.Length < 1) return; //No entities = no operations to perform
        NativeArray<Faction> factionArray = query.ToComponentDataArray<Faction>(Allocator.Temp);
        NativeArray<TargetOverride> targetOverrideArray = query.ToComponentDataArray<TargetOverride>(Allocator.Temp);

        //Get faction for targetted unit
        Faction targetedFaction = entityManager.GetComponentData<Faction>(hitEntity);

        for (int i = 0; i < targetOverrideArray.Length; i++)
        {
            //Copy of value, not reference. Setter must use entityManager.SetComponentData()
            TargetOverride newTargetOverride = targetOverrideArray[i];

            if (factionArray[i].factionID != targetedFaction.factionID)
            {
                newTargetOverride.targetEntity = hitEntity;
            }
            targetOverrideArray[i] = newTargetOverride;
            entityManager.SetComponentEnabled<MoveOverride>(entityArray[i], false);
        }
        query.CopyFromComponentDataArray(targetOverrideArray); //Remove when implementing single-entity instructions
    }

    /// <summary>
    /// Sets the movement destination for all UnitMover/MoveOverride Units selected
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    private void SetDestinationOnSelectedUnits(EntityManager entityManager)
    {
        Vector3 mouseWorldPosition = MouseWorldPosition.Instance.GetPosition();
        Vector3 targetPosition = mouseWorldPosition;

        //Query all entities with the UnitMover and Selected components to set their target
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<
            UnitMover,
            Selected,
            LocalTransform>().
            WithPresent<MoveOverride,TargetOverride>().Build(entityManager);

        //Register entities and components to modify in order to run Set on the original struct
        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        if (entityArray.Length < 1) return; //No entities = no operations to perform
        NativeArray<MoveOverride> moveOverrideArray = query.ToComponentDataArray<MoveOverride>(Allocator.Temp);
        NativeArray<TargetOverride> targetOverrideArray = query.ToComponentDataArray<TargetOverride>(Allocator.Temp);
        NativeArray<LocalTransform> localTransformArray = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        //Get average position of all entities queried to send it as start position to formation methods
        float3 avgPosition = float3.zero;
        avgPosition = AveragePosition(localTransformArray);

        //Calculate offset for each selected Unit inside a set formation.
        NativeArray<float3> formationPositionsArray = GenerateFormationPositionsArray(avgPosition, targetPosition, entityArray.Length);

        for (int i = 0; i < moveOverrideArray.Length; i++)
        {
            //New MoveOverride values
            MoveOverride newMoveOverride = moveOverrideArray[i];
            newMoveOverride.targetPosition = formationPositionsArray[i];
            moveOverrideArray[i] = newMoveOverride;
            entityManager.SetComponentEnabled<MoveOverride>(entityArray[i], true);

            //New TargetOverride values
            TargetOverride newTargetOverride = targetOverrideArray[i];
            newTargetOverride.targetEntity = Entity.Null;
            targetOverrideArray[i] = newTargetOverride;
            entityManager.SetComponentEnabled<MoveOverride>(entityArray[i], true);

            //[Deprecated]
            // Single-entity instruction alternative.
            /* entityManager.SetComponentData(entityArray[i], newUnitMover);  */

            // Writing a new local array and copying values to the query is preferable since it reduces writing operations.

        }
        query.CopyFromComponentDataArray(moveOverrideArray); //Remove when implementing single-entity instructions
        query.CopyFromComponentDataArray(targetOverrideArray); //Remove when implementing single-entity instructions
    }



    /// <summary>
    /// Retrieves a clicked-on Entity in the scene Collision (if any).
    /// </summary>
    // IDEA: Either convert to SphereCast or add a secondary larger collider used exclusively for selection
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
                CollidesWith = 1u << GameAssets.UNITS_LAYER,
                GroupIndex = 0
            }
        };

        //Query Raycast for a single Entity
        if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit raycastHit))
        {
            if (entityManager.ExistsAndPersists(raycastHit.Entity))
            {
                return raycastHit.Entity;
            }
        }
        return Entity.Null;
    }


    /// <summary>
    /// Calculates the average position of all LocalTransform components given.
    /// </summary>
    private static float3 AveragePosition(NativeArray<LocalTransform> localTransformArray)
    {
        float3 avgPosition = 0;
        if (localTransformArray.Length > 1)
        {
            float avgX = 0;
            float avgY = 0;
            float avgZ = 0;

            for (int i = 0; i < localTransformArray.Length; i++)
            {
                avgX += localTransformArray[i].Position.x;
                avgY += localTransformArray[i].Position.y;
                avgZ += localTransformArray[i].Position.z;
            }
            avgX /= localTransformArray.Length;
            avgY /= localTransformArray.Length;
            avgZ /= localTransformArray.Length;

            avgPosition = new float3(avgX, 0, avgZ);
        }
        else if (localTransformArray.Length == 1)
        {
            avgPosition = localTransformArray[0].Position;
        }
        else
        {
            throw new InvalidOperationException("Cannot calculate average of zero elements");
        }

        return avgPosition;
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
    /// TODO: Implement additional formations like Line, Square and Wedge.
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
