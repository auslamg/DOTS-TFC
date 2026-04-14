using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="FlowFieldPathRequest"/> unmanaged component.
/// </summary>
public class FlowFieldPathRequestAuthoring : MonoBehaviour
{
}

/// <summary>
/// Baker for the <see cref="FlowFieldPathRequest"/> unmanaged component.
/// </summary>
public class FlowFieldPathRequestBaker : Baker<FlowFieldPathRequestAuthoring>
{
    public override void Bake(FlowFieldPathRequestAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new FlowFieldPathRequest
        {
        });
        SetComponentEnabled<FlowFieldPathRequest>(entity, false);
    }
}

/// <summary>
/// Used by entities that request a specific FlowField path. 
/// </summary>
public struct FlowFieldPathRequest : IComponentData, IEnableableComponent
{
    public float3 targetPosition;
    public float3 lastMoveVector;
}

