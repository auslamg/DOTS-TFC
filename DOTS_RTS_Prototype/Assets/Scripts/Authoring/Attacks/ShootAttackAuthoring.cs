using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="ShootAttack"/> unmanaged component.
/// </summary>
class ShootAttackAuthoring : MonoBehaviour
{
    /// <summary>
    /// Time span between attacks.
    /// </summary>
    public float attackFrequency;
    /// <summary>
    /// Maximum distance for attacks.
    /// </summary>
    public float attackDistance;
    /// <summary>
    /// Base damage dealt with each attack.
    /// </summary>
    public int damageAmount;
    /// <summary>
    /// Spawn position for the projectile shot.
    /// </summary>
    /// <remarks>
    /// The transform is converted to local space from global space during baking process.
    /// </remarks>
    public Transform projectileSpawnPointTransform;
}

/// <summary>
/// Baker for the <see cref="ShootAttack"/> unmanaged component.
/// </summary>
class ShootAttackBaker : Baker<ShootAttackAuthoring>
{
    public override void Bake(ShootAttackAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ShootAttack
        {
            attackFrequency = authoring.attackFrequency,
            attackDistance = authoring.attackDistance,
            damageAmount = authoring.damageAmount,
            projectileSpawnPointLocalPosition = authoring.projectileSpawnPointTransform.localPosition
        });
    }
}

/// <summary>
/// Used by entities that can perform a projectile-based attack.
/// </summary>
/// <remarks>
/// Requires the <see cref="Targetter"/> component 
/// //IDEA: Enforce implementation through [RequireComponent(typeof(Targetter))]
/// </remarks>
public struct ShootAttack : IComponentData
{
    /// <summary>
    /// Current time passed since the last attack.
    /// </summary>
    public float attackPhaseTime;
    /// <summary>
    /// Time span between attacks.
    /// </summary>
    public float attackFrequency;
    /// <summary>
    /// Maximum distance for attacks.
    /// </summary>
    public float attackDistance;
    /// <summary>
    /// Base damage dealt with each attack.
    /// </summary>
    /// <remarks>
    /// The damage is passed onto the instanced projectile.
    /// </remarks>
    public int damageAmount;
    /// <summary>
    /// Spawn position for the projectile shot.
    /// </summary>
    /// <remarks>
    /// The transform is converted to local space from global space during baking process.
    /// </remarks>
    public float3 projectileSpawnPointLocalPosition;
    /// <summary>
    /// Event-struct triggered on shoot attack.
    /// </summary>
    /// <remarks>
    /// Custom events are reset at the end of each frame in ResetEventSystem.
    /// </remarks>
    public OnShootEvent onShoot;

    /// <summary>
    /// Event-struct triggered on shoot attack.
    /// </summary>
    /// <para> </para>
    /// <remarks>
    /// Custom events are reset at the end of each frame in ResetEventSystem.
    /// </remarks>
    public struct OnShootEvent
    {
        /// <summary>
        /// Event-bool triggered on melee attack.
        /// </summary>
        public bool isTriggered;
        /// <summary>
        /// Global-space position from which the shot originated.
        /// </summary>
        public float3 shootFromPosition;
    }
}
