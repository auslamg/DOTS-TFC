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
    public float attemptInterval;
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
            attemptCooldownTimer = new LoopingTimer
            {
                Interval = authoring.attemptInterval,
            }
        });
    }
}

/// <summary>
/// Used by entities that automatically lose their target after a period of time if the target is far enough. 
/// </summary>
/// <remarks>
/// Requires the <see cref="Targetter"/> component 
/// </remarks>
public struct LoseTarget : IComponentData
{
    /// <summary>
    /// Minimum distance threshold to consider losing the current target.
    /// </summary>
    public float thresholdDistance;
    /// <summary>
    /// Looping timer to wait between loss attempts.
    /// </summary>
    public LoopingTimer attemptCooldownTimer;
}
