using Unity.Entities;
using UnityEngine;

class FindTargetAuthoring : MonoBehaviour
{
    public float targetRange;
    public Faction targetFaction;
    public float scanFrequency;
    public float swapTargetMinDistance;
}

class FindTargetBaker : Baker<FindTargetAuthoring>
{
    public override void Bake(FindTargetAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new FindTarget
        {
            targetRange = authoring.targetRange,
            targetFaction = authoring.targetFaction,
            scanFrequency = authoring.scanFrequency,
            swapTargetMinDistance = authoring.swapTargetMinDistance
        });
    }
}

public struct FindTarget : IComponentData
{
    public float targetRange;
    public Faction targetFaction;
    public float scanPhaseTime;
    public float scanFrequency;
    public float swapTargetMinDistance;
}