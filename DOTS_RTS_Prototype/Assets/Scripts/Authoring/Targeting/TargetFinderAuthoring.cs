using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="TargetFinder"/> unmanaged component.
/// </summary>
class TargetFinderAuthoring : MonoBehaviour
{
    /// <summary>
    /// Time span between scans.
    /// </summary>
    public float scanFrequency;
    /// <summary>
    /// Maximum distance for acquiring targets.
    /// </summary>
    public float targetRange;
    /// <summary>
    /// Minimum distance between targets to consider swapping to the closest one.
    /// </summary>
    public float swapTargetMinDistance;
}

/// <summary>
/// Baker for the <see cref="TargetFinder"/> unmanaged component.
/// </summary>
class TargetFinderBaker : Baker<TargetFinderAuthoring>
{
    public override void Bake(TargetFinderAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new TargetFinder
        {
            targetRange = authoring.targetRange,
            scanFrequency = authoring.scanFrequency,
            swapTargetMinDistance = authoring.swapTargetMinDistance
        });
    }
}

/// <summary>
/// Used by entities that automatically scan for nearby entities to acquire a target whenever there is no target set. 
/// </summary>
/// <remarks>
/// Requires the <see cref="Targetter"/> component 
/// //IDEA: Enforce implementation through [RequireComponent(typeof(Targetter))]
/// </remarks>
public struct TargetFinder : IComponentData
{
    /// <summary>
    /// Current time passed since the last scan.
    /// </summary>
    public float scanPhaseTime;
    /// <summary>
    /// Time span between scans.
    /// </summary>
    public float scanFrequency;
    /// <summary>
    /// Maximum distance for acquiring targets.
    /// </summary>
    public float targetRange;
    /// <summary>
    /// Minimum distance between targets to consider swapping to the closest one.
    /// </summary>
    public float swapTargetMinDistance;
}