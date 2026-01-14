using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <c>AnimatedMeshReference</c> unmanaged component.
/// </summary>
class AnimatedMeshReferenceAuthoring : MonoBehaviour
{
    /// <summary>
    /// Pointer to the Entity that holds the animated mesh.
    /// </summary>
    public GameObject animatedMeshGameObject;
}

/// <summary>
/// Baker for the <c>AnimatedMeshReference</c> unmanaged component.
/// </summary>
class AnimatedMeshReferenceBaker : Baker<AnimatedMeshReferenceAuthoring>
{
    public override void Bake(AnimatedMeshReferenceAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new AnimatedMeshReference
        {
            meshEntity = GetEntity(authoring.animatedMeshGameObject, TransformUsageFlags.Dynamic)
        });
    }
}

public struct AnimatedMeshReference : IComponentData
{
    /// <summary>
    /// Pointer to the Entity that holds the animated mesh.
    /// </summary>
    public Entity meshEntity;
}
