using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="StraightPathRequest"/> unmanaged component.
/// </summary>
public class StraightPathRequestAuthoring : MonoBehaviour
{
}

/// <summary>
/// Baker for the <see cref="StraightPathRequest"/> unmanaged component.
/// </summary>
public class StraightPathRequestBaker : Baker<StraightPathRequestAuthoring>
{
    public override void Bake(StraightPathRequestAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new StraightPathRequest
        {
        });
        SetComponentEnabled<StraightPathRequest>(entity, false);
    }
}

/// <summary>
/// Used by entities that check if a pathing path is available.
/// </summary>
public struct StraightPathRequest : IComponentData, IEnableableComponent
{
    public float3 targetPosition;
}

