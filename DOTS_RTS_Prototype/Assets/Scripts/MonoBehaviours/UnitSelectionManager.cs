using Unity.Android.Gradle.Manifest;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

//TODO: Rename to UnitTargetManager if only target is handled.
public class UnitSelectionManager : MonoBehaviour
{
    /// <summary>
    /// Update() : MonoBehaviour
    /// </summary>
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mouseWorldPosition = MouseWorldPosition.Instance.GetPosition();

            //Query all entities with the UnitMover and Selected components to set their target
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<UnitMover, Selected>().Build(entityManager);

            //Register entities and components to modify in order to run Set on the original struct
            NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
            NativeArray<UnitMover> unitMoverArray = query.ToComponentDataArray<UnitMover>(Allocator.Temp);
            for (int i = 0; i < unitMoverArray.Length; i++)
            {
                //Copy of value, not reference. Setter must use entityManager.SetComponentData()
                UnitMover newUnitMover = unitMoverArray[i];
                newUnitMover.targetPosition = mouseWorldPosition;

                //[Deprecated]
                // Single-entity instruction alternative. Overwriting the local array and copying
                // values to the query is preferable since it reduces writing operations.
                /* entityManager.SetComponentData(entityArray[i], newUnitMover);  */
                unitMoverArray[i] = newUnitMover;
            }
            query.CopyFromComponentDataArray(unitMoverArray); //Remove when implementing single-entity instructions

        }
    }
}
