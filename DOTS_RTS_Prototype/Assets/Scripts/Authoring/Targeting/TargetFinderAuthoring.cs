using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="TargetFinder"/> unmanaged component.
/// </summary>
class TargetFinderAuthoring : MonoBehaviour
{
    /// <summary>
    /// Time interval between scans.
    /// </summary>
    [SerializeField]
    [Tooltip("Time interval between automatic target scans.")]
    public float scanFrequency;
    /// <summary>
    /// Maximum range for acquiring targets.
    /// </summary>
    [SerializeField]
    [Tooltip("Maximum range used when scanning for targets.")]
    public float targetRange;
    /// <summary>
    /// Minimum distance between targets to consider swapping to the closest one.
    /// </summary>
    [SerializeField]
    [Tooltip("Minimum distance improvement required before swapping to a closer target.")]
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
/// </remarks>
public struct TargetFinder : IComponentData
{
    /// <summary>
    /// Current time passed since the last scan.
    /// </summary>
    public float scanPhaseTime;
    /// <summary>
    /// Time interval between scans.
    /// </summary>
    public float scanFrequency;
    /// <summary>
    /// Maximum range for acquiring targets.
    /// </summary>
    public float targetRange;
    /// <summary>
    /// Minimum distance between targets to consider swapping to the closest one.
    /// </summary>
    public float swapTargetMinDistance;
}