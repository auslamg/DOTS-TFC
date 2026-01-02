using Unity.Entities;
using UnityEngine;

class SelfDestroyAuthoring : MonoBehaviour
{
    public float delay;
}

class SelfDestroyAuthoringBaker : Baker<SelfDestroyAuthoring>
{
    public override void Bake(SelfDestroyAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new SelfDestroy
        {
            delay = authoring.delay
        });
    }
}

public struct SelfDestroy : IComponentData
{
    public float delay;
}