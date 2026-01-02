using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

class ShootableAuthoring : MonoBehaviour
{
    public Transform hitPoint;
}

class ShootableAuthoringBaker : Baker<ShootableAuthoring>
{
    public override void Bake(ShootableAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        SetComponent(entity, new Shootable
        {
            hitPoint = authoring.hitPoint.localPosition
        });
    }
}

public struct Shootable : IComponentData
{
    public float3 hitPoint;
}
