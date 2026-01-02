using System.Reflection.Emit;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

partial struct ShootAttackSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        foreach ((
            RefRW<LocalTransform> localTransform,
            RefRW<ShootAttack> shootAttack,
            RefRO<Targetter> targetter,
            RefRW<UnitMover> unitMover)
                in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRW<ShootAttack>,
                RefRO<Targetter>,
                RefRW<UnitMover>
                >())
        {
            //FIX: Avoid continue. Maybe labels/goto?
            //If there is no target, go for next entity
            if (targetter.ValueRO.targetEntity == Entity.Null)
            {
                continue;
            }

            //FIX: Avoid continue. Maybe labels/goto?
            //DONE: FIX: Distance checks are only run on attack frequency instead of periodically
            //BUG: Distance checks override movement orders, because they rewrite target
            //If target is too far away to attack, get closer
            LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(targetter.ValueRO.targetEntity);
            if (math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position) > shootAttack.ValueRO.attackDistance)
            {
                //Too far, move closer
                unitMover.ValueRW.targetPosition = targetLocalTransform.Position;
                continue;
            }
            else
            {
                //Close enough, stop moving and attack
                unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;
                Debug.Log("STOPPED");
            }

            float3 aimDirection = targetLocalTransform.Position - localTransform.ValueRO.Position;
            aimDirection = math.normalize(aimDirection);

            //TODO: Snip for example doc [rotate into vector direction]
            quaternion aimRotation = quaternion.LookRotation(aimDirection, math.up());
            localTransform.ValueRW.Rotation = 
                math.slerp(localTransform.ValueRO.Rotation, aimRotation, SystemAPI.Time.DeltaTime * unitMover.ValueRO.rotationSpeed); //replace with aimRotation for no interpolation

            //TODO: Extract into AttackLoop() method or corroutine
            //IDEA: Refactor into corroutines
            //FIX: Avoid continue. Maybe labels/goto?
            //If there is a target and the attack phase is over, attack and restart phase.
            shootAttack.ValueRW.attackPhaseTime -= SystemAPI.Time.DeltaTime;
            if (shootAttack.ValueRO.attackPhaseTime > 0f)
            {
                continue;
            }
            shootAttack.ValueRW.attackPhaseTime = shootAttack.ValueRO.attackFrequency;

            //Instant damage
            /* RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(target.ValueRO.targetEntity);
            int damageAmount = 3;
            targetHealth.ValueRW.currentHealth -= damageAmount; */

            Entity bulletEntity = state.EntityManager.Instantiate(entitiesReferences.bulletPrefabEntity);
            float3 bulletSpawnPoint = localTransform.ValueRO.TransformPoint( shootAttack.ValueRO.bulletSpawnPointLocalPosition);
            SystemAPI.SetComponent(bulletEntity, LocalTransform.FromPosition(bulletSpawnPoint));

            RefRW<Bullet> bulletComponent = SystemAPI.GetComponentRW<Bullet>(bulletEntity);
            bulletComponent.ValueRW.damageAmount = shootAttack.ValueRO.damageAmount;

            RefRW<Targetter> bulletTarget = SystemAPI.GetComponentRW<Targetter>(bulletEntity);
            bulletTarget.ValueRW.targetEntity = targetter.ValueRO.targetEntity;
        }
    }
}
