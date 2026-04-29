using Unity.Entities;
using UnityEngine;

/// <summary>
/// Managed component for the <see cref="Building"/> unmanaged component.
/// </summary>
public class ResourceSourceAuthoring : MonoBehaviour
{
    public string generatedResourceKey;
}

/// <summary>
/// Baker for the <see cref="Building"/> unmanaged component.
/// </summary>
public class ResourceSourceBaker : Baker<ResourceSourceAuthoring>
{
    public override void Bake(ResourceSourceAuthoring authoring)
    {

        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ResourceSource
        {
            generatedResourceKey = new ResourceKey
            {
                name = authoring.generatedResourceKey,
            }
        });
    }
}

/// <summary>
/// Used by entities that represent a resource source.
/// </summary>
public struct ResourceSource : IComponentData
{
    public ResourceKey generatedResourceKey;
}