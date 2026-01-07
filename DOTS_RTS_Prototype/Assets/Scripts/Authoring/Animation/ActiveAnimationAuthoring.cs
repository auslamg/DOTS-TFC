using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

class ActiveAnimationAuthoring : MonoBehaviour
{
    public AnimationDataSO.AnimationType nextAnimationType;

}

class ActiveAnimationBaker : Baker<ActiveAnimationAuthoring>
{
    public override void Bake(ActiveAnimationAuthoring authoring)
    {
        EntitiesGraphicsSystem egs =
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EntitiesGraphicsSystem>();

        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ActiveAnimation
        {
            nextAnimationType = authoring.nextAnimationType
        });
    }
}

public struct ActiveAnimation : IComponentData
{
    public int currentFrame;
    public float framePhaseTime;
    public AnimationDataSO.AnimationType activeAnimationType;
    public AnimationDataSO.AnimationType nextAnimationType;
}
