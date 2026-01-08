using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// Managed component for the <c>ActiveAnimation</c> unmanaged component.
/// </summary>
class ActiveAnimationAuthoring : MonoBehaviour
{
    //TODO: Refactor. Undocumented. 
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
    /// <summary>
    /// Current frame index in frame MeshArray. 
    /// Indicates the state of the current animation iteration.
    /// </summary>
    public int currentFrame;
    /// <summary>
    /// Current time passed since the last frame change.
    /// Indicates the state of the current frame iteration.
    /// </summary>
    public float framePhaseTime;
    //TODO: Refactor. Undocumented. 
    public AnimationDataSO.AnimationType activeAnimationType;
    //TODO: Refactor. Undocumented. 
    public AnimationDataSO.AnimationType nextAnimationType;
}
