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
            if (target.ValueRO.targetEntity == Entity.Null)
            {
                continue;
            }

            ShootAttack.ValueRW.attackPhaseTime -= SystemAPI.Time.DeltaTime;
            if (ShootAttack.ValueRO.attackPhaseTime > 0f) 
            {
                continue; 
            }

            ShootAttack.ValueRW.attackPhaseTime = ShootAttack.ValueRO.attackFrequency;

            Debug.Log("Shoot");
        }
    }
}
