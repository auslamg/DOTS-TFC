using Unity.Entities;
using UnityEngine;

class BulletAuthoring : MonoBehaviour
{
    public float speed;
    public int damageAmount;
}

class BulletBaker : Baker<BulletAuthoring>
{
    public override void Bake(BulletAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Bullet
        {
            speed = authoring.speed,
            damageAmount = authoring.damageAmount
        });
    }
}

public struct Bullet : IComponentData
{
    public float speed;
    public int damageAmount;
}


