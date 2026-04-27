using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="PathRequest"/> unmanaged component.
/// </summary>
public class PathRequestAuthoring : MonoBehaviour
{
}

/// <summary>
/// Baker for the <see cref="PathRequest"/> unmanaged component.
/// </summary>
public class PathRequestBaker : Baker<PathRequestAuthoring>
{
    public override void Bake(PathRequestAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new PathRequest
        {
        });
        SetComponentEnabled<PathRequest>(entity, false);
    }
}

/// <summary>
/// Used by entities that check if a pathing path is available.
/// </summary>
public struct PathRequest : IComponentData, IEnableableComponent
{
    /// <summary>
    /// Current desired position to navigate to in case it is needed.
    /// </summary>
    public float3 targetPosition;
    /// <summary>
    /// Desired final position after formation calculation.
    /// </summary>
    public float3 postFormationPosition;
}

