using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <c>UnitAnimations</c> unmanaged component.
/// </summary>
//TODO: Refactor EXTENSIVELY
class UnitAnimationsAuthoring : MonoBehaviour
{
    public AnimationDataSO.AnimationType idleAnimationType;
    public AnimationDataSO.AnimationType walkAnimationType;
    public AnimationDataSO.AnimationType shootAnimationType;
    public AnimationDataSO.AnimationType aimAnimationType;
    public AnimationDataSO.AnimationType meleeAttackAnimationType;

}

class UnitAnimationsAuthoringBaker : Baker<UnitAnimationsAuthoring>
{
    public override void Bake(UnitAnimationsAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new UnitAnimations
        {
            idleAnimationType = authoring.idleAnimationType,
            walkAnimationType = authoring.walkAnimationType,
            shootAnimationType = authoring.shootAnimationType,
            aimAnimationType = authoring.aimAnimationType,
            meleeAttackAnimationType = authoring.meleeAttackAnimationType,
        });
    }
}
//TODO: Refactor EXTENSIVELY
public struct UnitAnimations : IComponentData
{
    public AnimationDataSO.AnimationType idleAnimationType;
    public AnimationDataSO.AnimationType walkAnimationType;
    public AnimationDataSO.AnimationType shootAnimationType;
    public AnimationDataSO.AnimationType aimAnimationType;
    public AnimationDataSO.AnimationType meleeAttackAnimationType;
}
