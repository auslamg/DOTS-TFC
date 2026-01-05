using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class MeleeAttackAuthoring : MonoBehaviour
{
    public float attackFrequency;
    public float attackDistance;
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
            attackDistance = authoring.attackDistance,
            damageAmount = authoring.damageAmount,
        });
    }
}

public struct MeleeAttack : IComponentData
{
    public float attackPhaseTime;
    public float attackFrequency;
    public float attackDistance;
    public int damageAmount;
}
