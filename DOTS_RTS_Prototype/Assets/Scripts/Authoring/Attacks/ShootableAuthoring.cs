using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

class ShootableAuthoring : MonoBehaviour
{
    public Transform hitPointTransform;
}

class ShootableBaker : Baker<ShootableAuthoring>
{
    public override void Bake(ShootableAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Shootable
        {
            hitPointPosition = authoring.hitPointTransform.localPosition
        });
    }
}

public struct Shootable : IComponentData
{
    public float3 hitPointPosition;
}
