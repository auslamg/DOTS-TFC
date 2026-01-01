using Unity.Entities;
using UnityEngine;

class FindTargetAuthoring : MonoBehaviour
{
    public float targetRange;
    public Faction targetFaction;
    public float scanFrequency;

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
            scanFrequency = authoring.scanFrequency
        });
    }
}

public struct FindTarget : IComponentData
{
    public float targetRange;
    public Faction targetFaction;
    public float scanPhaseTime;
    public float scanFrequency;
}