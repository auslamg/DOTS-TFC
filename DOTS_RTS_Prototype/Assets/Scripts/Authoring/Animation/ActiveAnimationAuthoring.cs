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
    public AnimationKey nextAnimationKey;
}

/// <summary>
/// Baker for the <c>ActiveAnimation</c> unmanaged component.
/// </summary>
class ActiveAnimationBaker : Baker<ActiveAnimationAuthoring>
{
    public override void Bake(ActiveAnimationAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ActiveAnimation
        {
            nextAnimationKey = authoring.nextAnimationKey
        });
    }
}

/// <summary>
/// Used for the execution of the current selected animation loop.
/// </summary>
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
    public AnimationKey activeAnimationKey;
    //TODO: Refactor. Undocumented. 
    public AnimationKey nextAnimationKey;
}
