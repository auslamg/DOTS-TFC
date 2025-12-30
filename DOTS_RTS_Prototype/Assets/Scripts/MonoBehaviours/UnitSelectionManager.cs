using System;
using Unity.Android.Gradle.Manifest;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;

//TODO: Rename to UnitTargetManager if only target is handled.
public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance { get; private set; }

    public event EventHandler OnSelectionAreaStart;
    public event EventHandler OnSelectionAreaEnd;

    private Vector2 selectionStartMousePosition;

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


            //TODO: Extract into SelectArea() method
            //Query all entities with the UnitMover and Selected components to check if they're inside the SelectionAreaRect
            query = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, Unit>().WithPresent<Selected>().Build(entityManager);

            Rect selectionAreaRect = GetSelectionAreaRect();

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

            OnSelectionAreaEnd?.Invoke(this, EventArgs.Empty);
        }

        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorldPosition = MouseWorldPosition.Instance.GetPosition();

            //Query all entities with the UnitMover and Selected components to set their target
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<UnitMover, Selected>().Build(entityManager);

            //Register entities and components to modify in order to run Set on the original struct
            /* NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp); */ //Add when implementing single-entity instructions
            NativeArray<UnitMover> unitMoverArray = query.ToComponentDataArray<UnitMover>(Allocator.Temp);
            for (int i = 0; i < unitMoverArray.Length; i++)
            {
                //Copy of value, not reference. Setter must use entityManager.SetComponentData()
                UnitMover newUnitMover = unitMoverArray[i];
                newUnitMover.targetPosition = mouseWorldPosition;

                //[Deprecated]
                // Single-entity instruction alternative.
                /* entityManager.SetComponentData(entityArray[i], newUnitMover);  */

                // Overwriting the local array and copying values to the query is preferable since it reduces writing operations.
                unitMoverArray[i] = newUnitMover;
            }
            query.CopyFromComponentDataArray(unitMoverArray); //Remove when implementing single-entity instructions

        }
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
}
