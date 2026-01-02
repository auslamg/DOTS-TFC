using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class ShootAttackAuthoring : MonoBehaviour
{
    public float attackFrequency;
    public int damageAmount;
    public float attackDistance;
    public Transform bulletSpawnPointTransform;
}

class ShootAttackBaker : Baker<ShootAttackAuthoring>
{
    public override void Bake(ShootAttackAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ShootAttack
        {
            attackFrequency = authoring.attackFrequency,
            damageAmount = authoring.damageAmount,
            attackDistance = authoring.attackDistance,
            bulletSpawnPointLocalPosition = authoring.bulletSpawnPointTransform.localPosition
        });
    }
}

public struct ShootAttack : IComponentData
{
    public float attackPhaseTime;
    public float attackFrequency;
    public int damageAmount;
    public float attackDistance;
    public float3 bulletSpawnPointLocalPosition;
    public OnShootEvent onShoot;

    public struct OnShootEvent
    {
        public bool isTriggered;
        public float3 shootFromPosition;
    }
}
