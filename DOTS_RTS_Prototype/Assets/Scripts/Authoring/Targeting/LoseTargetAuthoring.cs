using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="LoseTarget"/> unmanaged component.
/// </summary>
class LoseTargetAuthoring : MonoBehaviour
{
    /// <summary>
    /// Minimum distance threshold to consider losing the current target.
    /// </summary>
    [SerializeField]
    [Tooltip("Minimum distance threshold to consider losing the current target.")]
    public float thresholdDistance;
    /// <summary>
    /// Time interval between lose-target checks.
    /// </summary>
    [SerializeField]
    [Tooltip("Time interval between attempts to drop the current target.")]
    public float attemptFrequency;
}

/// <summary>
/// Baker for the <see cref="LoseTarget"/> unmanaged component.
/// </summary>
class LoseTargetBaker : Baker<LoseTargetAuthoring>
{
    public override void Bake(LoseTargetAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new LoseTarget
        {
            thresholdDistance = authoring.thresholdDistance,
            attemptFrequency = authoring.attemptFrequency
        });   
    }
}

/// <summary>
/// Used by entities that automatically lose their target after a period of time if the target is far enough. 
/// </summary>
/// <remarks>
/// Requires the <see cref="Targetter"/> component 
/// //IDEA: Enforce implementation through [RequireComponent(typeof(Targetter))]
/// </remarks>
public struct LoseTarget : IComponentData
{
    /// <summary>
    /// Minimum distance threshold to consider losing the current target.
    /// </summary>
    public float thresholdDistance;
    /// <summary>
    /// Current time passed since the last attempt.
    /// </summary>
    public float attemptPhaseTime;
    /// <summary>
    /// Time interval between lose-target checks.
    /// </summary>
    public float attemptFrequency;
}
