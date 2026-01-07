using Unity.Entities;
using UnityEngine;

class LoseTargetAuthoring : MonoBehaviour
{
    public float thresholdDistance;
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
    public float thresholdDistance;
    public float attemptPhaseTime;
    public float attemptFrequency;
}
