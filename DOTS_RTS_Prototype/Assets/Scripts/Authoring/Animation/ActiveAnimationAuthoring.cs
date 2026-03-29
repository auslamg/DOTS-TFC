using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// Managed component for the <see cref="ActiveAnimation"/> unmanaged component.
/// </summary>
class ActiveAnimationAuthoring : MonoBehaviour
{
    /// <summary>
    /// Animation requested at spawn time (and used as next requested animation state).
    /// </summary>
    [SerializeField]
    [Tooltip("Animation key requested at spawn and used as the next animation state.")]
    public AnimationKey nextAnimationKey;
}

/// <summary>
/// Baker for the <see cref="ActiveAnimation"/> unmanaged component.
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
    /// <summary>
    /// Animation currently being sampled to resolve frame mesh indices.
    /// </summary>
    public AnimationKey activeAnimationKey;
    /// <summary>
    /// Next requested animation key to transition into.
    /// </summary>
    public AnimationKey nextAnimationKey;
}
