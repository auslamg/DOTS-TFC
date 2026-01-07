using Unity.Entities;
using UnityEngine;

class AnimatedMeshReferenceAuthoring : MonoBehaviour
{
    public GameObject animatedMeshGameObject;
}

class AnimatedMeshReferenceAuthoringBaker : Baker<AnimatedMeshReferenceAuthoring>
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
    public Entity meshEntity;
}
