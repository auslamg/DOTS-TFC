using Unity.Entities;
using UnityEngine;

class TargetOverrideAuthoring : MonoBehaviour
{

}

class TargetOverrideAuthoringBaker : Baker<TargetOverrideAuthoring>
{
    public override void Bake(TargetOverrideAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new TargetOverride
        {
        });
    }
}

public struct TargetOverride : IComponentData
{
    public Entity targetEntity;
}