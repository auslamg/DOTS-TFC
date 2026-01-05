using Unity.Entities;
using UnityEngine;

class LoseTargetAuthoring : MonoBehaviour
{
    public float thresholdDistance;
}

class LoseTargetAuthoringBaker : Baker<LoseTargetAuthoring>
{
    public override void Bake(LoseTargetAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new LoseTarget
        {
            thresholdDistance = authoring.thresholdDistance
        });
        
    }
}

public struct LoseTarget : IComponentData
{
    public float thresholdDistance;
}
