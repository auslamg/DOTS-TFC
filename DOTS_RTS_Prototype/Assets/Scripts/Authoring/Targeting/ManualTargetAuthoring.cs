using Unity.Entities;
using UnityEngine;

class ManualTargetAuthoring : MonoBehaviour
{

}

class ManualTargetAuthoringBaker : Baker<ManualTargetAuthoring>
{
    public override void Bake(ManualTargetAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ManualTarget
        {
        });
    }
}

public struct ManualTarget : IComponentData
{
    public Entity targetEntity;
}