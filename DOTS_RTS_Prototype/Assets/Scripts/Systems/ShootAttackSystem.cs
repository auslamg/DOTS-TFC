using System.Reflection.Emit;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

partial struct ShootAttackSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRW<ShootAttack> ShootAttack,
            RefRO<Target> target)
                in SystemAPI.Query<
                RefRW<ShootAttack>,
                RefRO<Target>
                >())
        {
            //FIX: Avoid continue. Maybe labels/goto?
            //If there is no target, go for next entity
            if (target.ValueRO.targetEntity == Entity.Null)
            {
                continue;
            }

            //IDEA: Refactor into corroutines
            //FIX: Avoid continue. Maybe labels/goto?
            //If there is a target and the attack phase is over, attack and restart phase.
            ShootAttack.ValueRW.attackPhaseTime -= SystemAPI.Time.DeltaTime;
            if (ShootAttack.ValueRO.attackPhaseTime > 0f) 
            {
                continue; 
            }
            ShootAttack.ValueRW.attackPhaseTime = ShootAttack.ValueRO.attackFrequency;

            RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(target.ValueRO.targetEntity);
            int damageAmount = 3;
            targetHealth.ValueRW.currentHealth -= damageAmount;
        }
    }
}
