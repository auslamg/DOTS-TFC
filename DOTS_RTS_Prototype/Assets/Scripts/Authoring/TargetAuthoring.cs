using Unity.Entities;
using UnityEngine;

class TargetAuthoring : MonoBehaviour
{

}

class TargetBaker : Baker<TargetAuthoring>
{
    public override void Bake(TargetAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Target
        {

        });
    }
}

public struct Target : IComponentData
{
    public Entity targetEntity;
}