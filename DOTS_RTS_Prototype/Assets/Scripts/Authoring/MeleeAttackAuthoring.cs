using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class MeleeAttackAuthoring : MonoBehaviour
{
    public float attackFrequency;
    public float attackDistanceSquared;
    public int damageAmount;

}

class MeleeAttackBaker : Baker<MeleeAttackAuthoring>
{
    public override void Bake(MeleeAttackAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new MeleeAttack
        {
            attackFrequency = authoring.attackFrequency,
            attackDistanceSquared = authoring.attackDistanceSquared,
            damageAmount = authoring.damageAmount,
        });
    }
}

public struct MeleeAttack : IComponentData
{
    public float attackPhaseTime;
    public float attackFrequency;
    public float attackDistanceSquared;
    public int damageAmount;
}
