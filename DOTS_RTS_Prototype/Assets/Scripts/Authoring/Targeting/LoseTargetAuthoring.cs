using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <c>LoseTarget</c> unmanaged component.
/// </summary>
class LoseTargetAuthoring : MonoBehaviour
{
    /// <summary>
    /// Minimum distance difference to target to consider losing it.
    /// </summary>
    public float thresholdDistance;
    /// <summary>
    /// Time span between attempts.
    /// </summary>
    public float attemptFrequency;
}

class LoseTargetAuthoringBaker : Baker<LoseTargetAuthoring>
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

public struct LoseTarget : IComponentData
{
    /// <summary>
    /// Minimum distance difference to target to consider losing it.
    /// </summary>
    public float thresholdDistance;
    /// <summary>
    /// Current time passed since the last attempt.
    /// </summary>
    public float attemptPhaseTime;
    /// <summary>
    /// Time span between attempts.
    /// </summary>
    public float attemptFrequency;
}
