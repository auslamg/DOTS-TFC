using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
/// <summary>
/// Managed component for the <c>Shootable</c> unmanaged component.
/// </summary>
class ShootableAuthoring : MonoBehaviour
{
    /// <summary>
    /// Projectile destination position.
    /// Projectiles aimed at this entity will aim exactly at this point.
    /// </summary>
    /// <remarks>
    /// The transform is converted to local space from global space during baking process.
    /// </remarks>
    public Transform hitPointTransform;
}

/// <summary>
/// Baker for the <c>Shootable</c> unmanaged component.
/// </summary>
class ShootableBaker : Baker<ShootableAuthoring>
{
    public override void Bake(ShootableAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Shootable
        {
            hitPointPosition = authoring.hitPointTransform.localPosition
        });
    }
}

public struct Shootable : IComponentData
{
    /// <summary>
    /// Projectile destination position.
    /// Projectiles aimed at this entity will aim exactly at this point.
    /// </summary>
    /// <remarks>
    /// The transform is converted to local space from global space during baking process.
    /// </remarks>
    public float3 hitPointPosition;
}
