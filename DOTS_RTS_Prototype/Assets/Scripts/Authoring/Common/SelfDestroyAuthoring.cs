using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="SelfDestroy"/> unmanaged component.
/// </summary>
class SelfDestroyAuthoring : MonoBehaviour
{
    /// <summary>
    /// Time delay in seconds before object self-destruction.
    /// </summary>
    [SerializeField]
    [Tooltip("Delay in seconds before this entity is destroyed.")]
    public float delay;
}

/// <summary>
/// Baker for the <see cref="SelfDestroy"/> unmanaged component.
/// </summary>
class SelfDestroyBaker : Baker<SelfDestroyAuthoring>
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

/// <summary>
/// Used by entities that should be destroyed after a delay.
/// </summary>
public struct SelfDestroy : IComponentData
{
    /// <summary>
    /// Time delay in seconds before object self-destruction.
    /// </summary>
    public float delay;
}