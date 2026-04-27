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
    /// <summary>
    /// Current desired position to navigate to in case it is needed.
    /// </summary>
    public float3 targetPosition;
    /// <summary>
    /// Last used <see cref="FlowField"/> vector to reuse in case a flowfield is unavailable (like inside walls).
    /// </summary>
    public float3 lastFlowVector;
    /// <summary>
    /// Desired final position after formation calculation.
    /// </summary>
    public float3 postFormationPosition;

}

