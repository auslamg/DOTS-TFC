using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="FlowFieldRequest"/> unmanaged component.
/// </summary>
public class FlowFieldRequestAuthoring : MonoBehaviour
{
}

/// <summary>
/// Baker for the <see cref="FlowFieldRequest"/> unmanaged component.
/// </summary>
public class FlowFieldRequestBaker : Baker<FlowFieldRequestAuthoring>
{
    public override void Bake(FlowFieldRequestAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new FlowFieldRequest
        {
        });
        SetComponentEnabled<FlowFieldRequest>(entity, false);
    }
}

/// <summary>
/// Used by entities that request a specific FlowField. 
/// </summary>
public struct FlowFieldRequest : IComponentData, IEnableableComponent
{
    public float3 targetPosition;
    public float3 lastMoveVector;
}

