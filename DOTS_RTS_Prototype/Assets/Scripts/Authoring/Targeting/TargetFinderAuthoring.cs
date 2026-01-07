using Unity.Entities;
using UnityEngine;

class TargetFinderAuthoring : MonoBehaviour
{
    public float targetRange;
    public float scanFrequency;
    public float swapTargetMinDistance;
}

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

public struct TargetFinder : IComponentData
{
    public float targetRange;
    public float scanPhaseTime;
    public float scanFrequency;
    public float swapTargetMinDistance;
}