using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="UnitAnimations"/> unmanaged component.
/// </summary>
class UnitAnimationsAuthoring : MonoBehaviour
{
    /// <summary>
    /// Fallback animation key used when no specific state key is selected.
    /// </summary>
    [SerializeField]
    [Tooltip("Fallback animation key used when no specific animation state is selected.")]
    public string noneAnimationKey;
    /// <summary>
    /// Animation key used while the unit is idle.
    /// </summary>
    [SerializeField]
    [Tooltip("Animation key used while the unit is idle.")]
    public string idleAnimationKey;
    /// <summary>
    /// Animation key used while the unit is moving.
    /// </summary>
    [SerializeField]
    [Tooltip("Animation key used while the unit is moving.")]
    public string walkAnimationKey;
    /// <summary>
    /// Animation key used while performing melee attacks.
    /// </summary>
    [SerializeField]
    [Tooltip("Animation key used while performing melee attacks.")]
    public string meleeAttackAnimationKey;
    /// <summary>
    /// Animation key used while firing projectiles.
    /// </summary>
    [SerializeField]
    [Tooltip("Animation key used while firing projectiles.")]
    public string shootAnimationKey;
    /// <summary>
    /// Animation key used while aiming at a target.
    /// </summary>
    [SerializeField]
    [Tooltip("Animation key used while aiming at a target.")]
    public string aimAnimationKey;
}

/// <summary>
/// Baker for the <see cref="UnitAnimations"/> unmanaged component.
/// </summary>
class UnitAnimationsBaker : Baker<UnitAnimationsAuthoring>
{
    public override void Bake(UnitAnimationsAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new UnitAnimations
        {
            noneAnimationKey = new AnimationKey
            {
                name = authoring.noneAnimationKey,
            },

            idleAnimationKey = new AnimationKey
            {
                name = authoring.idleAnimationKey,
            },

            walkAnimationKey = new AnimationKey
            {
                name = authoring.walkAnimationKey,
            },

            meleeAttackAnimationKey = new AnimationKey
            {
                name = authoring.meleeAttackAnimationKey,
            },

            shootAnimationKey = new AnimationKey
            {
                name = authoring.shootAnimationKey,
            },

            aimAnimationKey = new AnimationKey
            {
                name = authoring.aimAnimationKey,
            },
        });
    }
}
/// <summary>
/// Contains the keys for the animations of a unit.
/// </summary>
public struct UnitAnimations : IComponentData
{
    /// <summary>
    /// Fallback animation key used when no specific state key is selected.
    /// </summary>
    public AnimationKey noneAnimationKey;
    /// <summary>
    /// Animation key used while the unit is idle.
    /// </summary>
    public AnimationKey idleAnimationKey;
    /// <summary>
    /// Animation key used while the unit is moving.
    /// </summary>
    public AnimationKey walkAnimationKey;
    /// <summary>
    /// Animation key used while firing projectiles.
    /// </summary>
    public AnimationKey shootAnimationKey;
    /// <summary>
    /// Animation key used while aiming at a target.
    /// </summary>
    public AnimationKey aimAnimationKey;
    /// <summary>
    /// Animation key used while performing melee attacks.
    /// </summary>
    public AnimationKey meleeAttackAnimationKey;
}
