using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <c>ManualMove</c> unmanaged component.
/// </summary>
public class ManualMoveAuthoring : MonoBehaviour
{
}

public class MoveOverrideBaker : Baker<ManualMoveAuthoring>
{
    public override void Bake(ManualMoveAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ManualMove
        {
            targetPosition = authoring.transform.position
        });
        SetComponentEnabled<ManualMove>(entity,false);
    }
}

public struct ManualMove : IComponentData, IEnableableComponent
{
    //TODO: Rename to destination
    /// <summary>
    /// Desired position to move the Entity to.
    /// </summary>
    public float3 targetPosition;
}

