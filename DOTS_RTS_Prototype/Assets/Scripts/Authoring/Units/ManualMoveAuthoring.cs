using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="ManualMove"/> unmanaged component.
/// </summary>
public class ManualMoveAuthoring : MonoBehaviour
{
}

/// <summary>
/// Baker for the <see cref="ManualMove"/> unmanaged component.
/// </summary>
public class ManualMoveBaker : Baker<ManualMoveAuthoring>
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

/// <summary>
/// Used by entities that allow for the manual selection of a movement destination. 
/// </summary>
/// <remarks>
/// Requires the <see cref="UnitMover"/> component 
/// </remarks>
public struct ManualMove : IComponentData, IEnableableComponent
{
    //TODO: Rename to destination
    /// <summary>
    /// Desired position to move the Entity to.
    /// </summary>
    public float3 targetPosition;
}

