using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="Targetter"/> unmanaged component.
/// </summary>
class TargetterAuthoring : MonoBehaviour
{
    /// <summary>
    /// Targeted GameObject baked to an Entity.
    /// </summary>
    /// <remarks>
    /// Entities with this component will usually have their own means for acquiring targets, so this field is mainly for testing purposes.
    /// </remarks>
    [SerializeField]
    [Tooltip("Optional test target GameObject converted to the initial target entity.")]
    public GameObject testTargetGameObject;

}

/// <summary>
/// Baker for the <see cref="Targetter"/> unmanaged component.
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

/// <summary>
/// Used by entities that can acquire a target. Mainly used for attacks, but it should be implemented in any additional AI behaviours based around another entity's position (follow, defend, surround, etc.)
/// </summary>
public struct Targetter : IComponentData
{
    /// <summary>
    /// Targeted entity.
    /// </summary>
    public Entity targetEntity;
}