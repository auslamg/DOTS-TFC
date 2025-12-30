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

//TODO: Rename to UnitTargetManager if only target is handled.
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
            for (int i = 0; i < entityArray.Length; i++)
            {
                entityManager.SetComponentEnabled<Selected>(entityArray[i], false);
            }

            //TODO: Extract into SelectSingle() method

            Rect selectionAreaRect = GetSelectionAreaRect();
            float selectionAreaSize = selectionAreaRect.width + selectionAreaRect.height;
            float multipleSelectionSizeMinimum = 40f;
            bool isMultipleSelection = selectionAreaSize >= multipleSelectionSizeMinimum;
            Debug.Log(isMultipleSelection + " " + selectionAreaSize);

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
                    }
                }
            }
            else
            {
                //TODO: Extract into SingleSelect() method
                //query = entityManager.CreateEntityQuery(typeof (PhysicsWorldSingleton)); //TODO: Alternative. Remove.

                //Register CollisionWorld for physics queries
                query = new EntityQueryBuilder(Allocator.Temp).WithAll<PhysicsWorldSingleton>().Build(entityManager);
                PhysicsWorldSingleton physiscsWorldSingleton = query.GetSingleton<PhysicsWorldSingleton>();
                CollisionWorld collisionWorld = physiscsWorldSingleton.CollisionWorld;

                //Build raycast from mouse position in appropiate layers
                UnityEngine.Ray cameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                int unitsLayer = 6;
                RaycastInput raycastInput = new RaycastInput
                {
                    Start = cameraRay.GetPoint(0f),
                    End = cameraRay.GetPoint(5000f),
                    Filter = new CollisionFilter
                    {
                        BelongsTo = ~0u, //All layers
                        CollidesWith = 1u << unitsLayer,
                        GroupIndex = 0
                    }
                };

                //Query Raycast for a single Unit Entity
                if (collisionWorld.CastRay(raycastInput, out Unity.Physics.RaycastHit raycastHit))
                {
                    //If a Unit Entity was hit 
                    if (entityManager.HasComponent<Unit>(raycastHit.Entity))
                    {
                        entityManager.SetComponentEnabled<Selected>(raycastHit.Entity, true);
                    }
                }

            }


            OnSelectionAreaEnd?.Invoke(this, EventArgs.Empty);
        }

        if (Input.GetMouseButtonDown(1))
        {
            //TODO: Extract into MoveUnits() method
            Vector3 mouseWorldPosition = MouseWorldPosition.Instance.GetPosition();
            Vector3 targetPosition = mouseWorldPosition;

            //Query all entities with the UnitMover and Selected components to set their target
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<UnitMover, Selected, LocalTransform>().Build(entityManager);

            //Register entities and components to modify in order to run Set on the original struct
            NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
            NativeArray<UnitMover> unitMoverArray = query.ToComponentDataArray<UnitMover>(Allocator.Temp);


            //Get average position of all entities queried to send it as start position
            float3 avgPosition = float3.zero;
            NativeArray<LocalTransform> localTransformArray = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            avgPosition = AveragePosition(localTransformArray);

            NativeArray<float3> movePositionArray = GenerateMovePositionArray(avgPosition, targetPosition, entityArray.Length);
            for (int i = 0; i < unitMoverArray.Length; i++)
            {
                //Copy of value, not reference. Setter must use entityManager.SetComponentData()
                UnitMover newUnitMover = unitMoverArray[i];
                newUnitMover.targetPosition = movePositionArray[i];

                //[Deprecated]
                // Single-entity instruction alternative.
                /* entityManager.SetComponentData(entityArray[i], newUnitMover);  */

                // Overwriting the local array and copying values to the query is preferable since it reduces writing operations.
                unitMoverArray[i] = newUnitMover;
            }
            query.CopyFromComponentDataArray(unitMoverArray); //Remove when implementing single-entity instructions

        }
    }

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

    private NativeArray<float3> GenerateMovePositionArray(float3 startPosition, float3 targetPosition, int positionCount)
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

    private NativeArray<float3> CalculateLineFormation(NativeArray<float3> positionArray, float3 startPosition, float3 targetPosition, int positionCount)
    {
        float offest = unitOffset;
        float3 targetDirection = targetPosition - startPosition;
        int positionIndex = 0;

        //Calculate angle for proper orientation
        float3 directionNormalized = math.normalize(targetDirection);
        float angle = math.atan2(directionNormalized.x, directionNormalized.z);
        /* Debug.Log("Angle: " + math.degrees(angle)); */

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

            //TODO: Refactor into no break use
            if (positionIndex >= positionCount)
            {
                break;
            }
        }
        return positionArray;
    }


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

                //TODO: Refactor into no break use
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
