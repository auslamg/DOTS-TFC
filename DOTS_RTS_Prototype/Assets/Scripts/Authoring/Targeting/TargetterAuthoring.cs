using Unity.Entities;
using UnityEngine;

class TargetterAuthoring : MonoBehaviour
{
    public GameObject testTargetGameObject;

}

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
    public Entity targetEntity;
}