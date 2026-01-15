using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="Shootable"/> unmanaged component.
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
/// Baker for the <see cref="Shootable"/> unmanaged component.
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

/// <summary>
/// Used by entities that get shot at by ShootAttack entities at a specific position.
/// </summary>
/// <remarks>
/// Usually <see cref="ShootAttack"/> will aim projectiles at the mass center of a target whenever it doesn't have this component.
/// The component, which must be added on the target entity (and not the shooter) will override the projectile's desired
/// destination from the center of mass to the specified <c>hitPointPosition</c>.
/// </remarks>
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
