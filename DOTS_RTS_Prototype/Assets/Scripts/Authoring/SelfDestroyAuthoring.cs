using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <c>SelfDestroy</c> unmanaged component.
/// </summary>
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