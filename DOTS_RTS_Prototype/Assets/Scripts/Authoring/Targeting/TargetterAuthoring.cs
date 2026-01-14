using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <c>Targetter</c> unmanaged component.
/// </summary>
class TargetterAuthoring : MonoBehaviour
{
    /// <summary>
    /// Targetted GameObject baked to an Entity.
    /// </summary>
    /// <remarks>
    /// Entities with this component will usually have their own means for acquiring targets, so this field is mainly for testing purposes.
    /// </remarks>
    public GameObject testTargetGameObject;

}

/// <summary>
/// Baker for the <c>Targetter</c> unmanaged component.
/// </summary>
class TargetterBaker : Baker<TargetterAuthoring>
{
    public override void Bake(TargetterAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Targetter
        {
            targetEntity = GetEntity(authoring.testTargetGameObject, TransformUsageFlags.Dynamic)
        });
    }
}

public struct Targetter : IComponentData
{
    /// <summary>
    /// Targetted entity.
    /// </summary>
    public Entity targetEntity;
}