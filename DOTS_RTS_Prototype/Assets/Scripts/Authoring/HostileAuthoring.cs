using Unity.Entities;
using UnityEngine;

class HostileAuthoring : MonoBehaviour
{

}

class HostileBaker : Baker<HostileAuthoring>
{
    public override void Bake(HostileAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Hostile
        {

        });
    }
}

public struct Hostile : IComponentData
{

}
