using Unity.Entities;
using UnityEngine;

/// <summary>
/// Managed component for the <c>Projectile</c> unmanaged component.
/// </summary>
/// <remarks>
/// Managed fields are used exclusively for in-scene pre-built GameObject testing,
/// and they are meant to be written real-time when intantiating a <c>Projectile</c> component.
/// </remarks>
class ProjectileAuthoring : MonoBehaviour
{
    /// <summary>
    /// The movement speed of the projectile.
    /// </summary>
    public float speed;
    /// <summary>
    /// The damage dealt to the target on contact.
    /// </summary>
    public int damageAmount;
}

/// <summary>
/// Baker for the <c>Projectile</c> unmanaged component.
/// </summary>
/// <remarks>
/// Managed fields are used exclusively for in-scene pre-built GameObject testing,
/// and they are meant to be written real-time when intantiating a <c>Projectile</c> component
/// </remarks>
class ProjectileBaker : Baker<ProjectileAuthoring>
{
    public override void Bake(ProjectileAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Projectile
        {
            speed = authoring.speed,
            damageAmount = authoring.damageAmount
        });
    }
}

/// <summary>
/// <c>Projectile</c> entity component.
/// Used for straight-path projectiles shot by ShootAttack component.
/// Must set values for all fields on instantiation.
/// </summary>
public struct Projectile : IComponentData
{
    /// <summary>
    /// The movement speed of the projectile.
    /// </summary>
    public float speed;
    /// <summary>
    /// The damage dealt to the target on contact.
    /// </summary>
    public int damageAmount;
    /// <summary>
    /// The Entity that spawned the projectile.
    /// </summary>
    public Entity shooterEntity;
}


