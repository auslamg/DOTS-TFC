using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="AnimatedMeshReference"/> unmanaged component.
/// </summary>
class AnimatedMeshReferenceAuthoring : MonoBehaviour
{
    /// <summary>
    /// Pointer to the Entity that holds the animated mesh.
    /// </summary>
    public GameObject animatedMeshGameObject;
}

/// <summary>
/// Baker for the <see cref="AnimatedMeshReference"/> unmanaged component.
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

/// <summary>
/// Used to reference a child entity containing the animated mesh from the parent.
/// </summary>
public struct AnimatedMeshReference : IComponentData
{
    /// <summary>
    /// Pointer to the Entity that holds the animated mesh.
    /// </summary>
    public Entity meshEntity;
}
