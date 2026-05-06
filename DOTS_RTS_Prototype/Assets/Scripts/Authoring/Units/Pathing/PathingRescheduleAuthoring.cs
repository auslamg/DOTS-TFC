using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Managed component for the <see cref="PathingReschedule"/> unmanaged component.
/// </summary>
class PathingRescheduleAuthoring : MonoBehaviour
{
    /// <summary>
    /// Time interval between reschedule attempts.
    /// </summary>
    [SerializeField]
    [Tooltip("Time interval between reschedule attempts.")]
    public float attemptInterval;
}

/// <summary>
/// Baker for the <see cref="PathingReschedule"/> unmanaged component.
/// </summary>
class PathingRescheduleBaker : Baker<PathingRescheduleAuthoring>
{
    public override void Bake(PathingRescheduleAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new PathingReschedule
        {
            attemptTimer = new LoopingTimer
            {
                Interval = authoring.attemptInterval
            }
        });
    }
}

/// <summary>
/// Used by entities that can reschedule a pathing request after cost changes.
/// </summary>
public struct PathingReschedule : IComponentData
{
    /// <summary>
    /// Looping timer to wait between reschedule attempts.
    /// </summary>
    public LoopingTimer attemptTimer;
}
