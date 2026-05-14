using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="FlowFieldFollower"/> unmanaged component.
/// </summary>
public class FlowFieldFollowerAuthoring : MonoBehaviour
{
}

/// <summary>
/// Baker for the <see cref="FlowFieldFollower"/> unmanaged component.
/// </summary>
public class FlowFieldFollowerBaker : Baker<FlowFieldFollowerAuthoring>
{
    public override void Bake(FlowFieldFollowerAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new FlowFieldFollower
        {
        });
        SetComponentEnabled<FlowFieldFollower>(entity, false);
    }
}

/// <summary>
/// Used by entities that follow a FlowField path. 
/// </summary>
public struct FlowFieldFollower : IComponentData, IEnableableComponent
{
    /// <summary>
    /// The current destination position of the entity.
    /// </summary>
    public float3 targetPosition;

    /// <summary>
    /// The last movement direction applied to the entity.
    /// </summary>
    public float3 lastMoveVector;

    /// <summary>
    /// The calculated formation-adjusted position.
    /// </summary>
    public float3 postFormationPosition;

    /// <summary>
    /// The index of the assigned flow field.
    /// </summary>
    public int flowFieldIndex;
}

