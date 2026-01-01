using Unity.Entities;
using UnityEngine;

class ShootAttackAuthoring : MonoBehaviour
{
    public float attackFrequency;
}

class ShootAttackBaker : Baker<ShootAttackAuthoring>
{
    public override void Bake(ShootAttackAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ShootAttack
        {
            attackFrequency = authoring.attackFrequency
        });
    }
}

public struct ShootAttack : IComponentData
{
    public float attackPhaseTime;
    public float attackFrequency;
}
