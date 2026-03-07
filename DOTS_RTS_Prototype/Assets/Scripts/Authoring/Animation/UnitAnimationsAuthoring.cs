using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="UnitAnimations"/> unmanaged component.
/// </summary>
//TODO: Refactor EXTENSIVELY
class UnitAnimationsAuthoring : MonoBehaviour
{
    public string noneAnimationKey;
    public string idleAnimationKey;
    public string walkAnimationKey;
    public string meleeAttackAnimationKey;
    public string shootAnimationKey;
    public string aimAnimationKey;
    //TODO: Bake bool playFull for custom animations

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
/// //REVIEW: Rename into HumanoidAnimations if other non-humanoid unit types are implemented
public struct UnitAnimations : IComponentData
{
    public AnimationKey noneAnimationKey;
    public AnimationKey idleAnimationKey;
    public AnimationKey walkAnimationKey;
    public AnimationKey shootAnimationKey;
    public AnimationKey aimAnimationKey;
    public AnimationKey meleeAttackAnimationKey;
}
