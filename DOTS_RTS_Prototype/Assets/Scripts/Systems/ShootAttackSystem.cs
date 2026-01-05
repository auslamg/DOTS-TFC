using System.Reflection.Emit;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

partial struct ShootAttackSystem : ISystem
{
    [BurstCompile]
    private void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferences>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Used for prefab instancing
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        
        foreach ((
            RefRW<LocalTransform> localTransform,
            RefRW<ShootAttack> shootAttack,
            RefRO<Targetter> targetter,
            RefRW<UnitMover> unitMover,
            Entity entity)
                in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRW<ShootAttack>,
                RefRO<Targetter>,
                RefRW<UnitMover>>().
                WithDisabled<MoveOverride>().
                WithEntityAccess())
        {
            //FIX: Avoid continue. Maybe labels/goto?
            //If there is no target, go for next entity
            if (!state.EntityManager.ExistsAndPersists(targetter.ValueRO.targetEntity))
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

            //Spawn projectile calculating global position
            Entity projectileEntity = state.EntityManager.Instantiate(entitiesReferences.bulletPrefabEntity);
            float3 projectileSpawnPoint = localTransform.ValueRO.TransformPoint( shootAttack.ValueRO.projectileSpawnPointLocalPosition);
            SystemAPI.SetComponent(projectileEntity, LocalTransform.FromPosition(projectileSpawnPoint));

            //Set spawned projectile values
            RefRW<Projectile> projectile = SystemAPI.GetComponentRW<Projectile>(projectileEntity);
            projectile.ValueRW.damageAmount = shootAttack.ValueRO.damageAmount;
            projectile.ValueRW.shooterEntity = entity;
            RefRW<Targetter> projectileTarget = SystemAPI.GetComponentRW<Targetter>(projectileEntity);
            projectileTarget.ValueRW.targetEntity = targetter.ValueRO.targetEntity;

            shootAttack.ValueRW.onShoot.isTriggered = true;
            shootAttack.ValueRW.onShoot.shootFromPosition = projectileSpawnPoint;
        }
    }
}
