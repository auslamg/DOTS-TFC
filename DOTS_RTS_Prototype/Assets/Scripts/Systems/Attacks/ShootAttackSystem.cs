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
        state.RequireForUpdate<EntityPrefabsRegistry>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Used for prefab instancing
        DynamicBuffer<EntityPrefab> entityReferencesBuffer = SystemAPI.GetBuffer<EntityPrefab>(
            SystemAPI.GetSingletonEntity<EntityPrefabsRegistry>());

        //Logic for MOVING shooters
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
                WithDisabled<ManualMove>().
                WithEntityAccess())
        {
            //If there is no target, go for next entity
            if (EntityUtil.ExistsAndPersists(ref state, targetter.ValueRO.targetEntity))
            {
                LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(targetter.ValueRO.targetEntity);

                //Target is too far, move closer
                if (math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position) > shootAttack.ValueRO.attackDistance)
                {
                    unitMover.ValueRW.targetPosition = targetLocalTransform.Position;
                    continue;
                }
                else
                {
                    //Close enough, stop moving and attack
                    unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;

                    float3 aimDirection = targetLocalTransform.Position - localTransform.ValueRO.Position;
                    aimDirection = math.normalize(aimDirection);

                    //TODO: Snip for example in documentation [rotate into vector direction]
                    quaternion aimRotation = quaternion.LookRotation(aimDirection, math.up());
                    localTransform.ValueRW.Rotation =
                        math.slerp(localTransform.ValueRO.Rotation, aimRotation, SystemAPI.Time.DeltaTime * unitMover.ValueRO.rotationSpeed); //replace with aimRotation for no interpolation
                }
            }
        }

        foreach ((
            RefRW<LocalTransform> localTransform,
            RefRW<ShootAttack> shootAttack,
            RefRO<Targetter> targetter,
            Entity entity)
                in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRW<ShootAttack>,
                RefRO<Targetter>>().
                WithEntityAccess())
        {
            //If there is no target, go for next entity
            if (EntityUtil.ExistsAndPersists(ref state, targetter.ValueRO.targetEntity))
            {
                LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(targetter.ValueRO.targetEntity);

                //Target is too far
                if (math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position) > shootAttack.ValueRO.attackDistance)
                {
                    continue;
                }

                //Entity is busy manually moving
                if (SystemAPI.HasComponent<ManualMove>(entity) &&
                    SystemAPI.IsComponentEnabled<ManualMove>(entity))
                {
                    continue;
                }
                
                {
                    //IDEA: Refactor into corroutines
                    //Timer
                    shootAttack.ValueRW.attackPhaseTime -= SystemAPI.Time.DeltaTime;
                    if (shootAttack.ValueRO.attackPhaseTime <= 0f)
                    {
                        shootAttack.ValueRW.attackPhaseTime = shootAttack.ValueRO.attackFrequency;

                        //Instant damage alternative
                        /* RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(target.ValueRO.targetEntity);
                        int damageAmount = 3;
                        targetHealth.ValueRW.currentHealth -= damageAmount; */

                        //Retrieve projectile prefab entity from EntityReferenceKey
                        Entity projectilePrefab = DataLookup.GetEntityPrefab(ref entityReferencesBuffer, shootAttack.ValueRO.projectilePrefabKey);

                        //Spawn projectile calculating global position
                        Entity projectileEntity = state.EntityManager.Instantiate(projectilePrefab);
                        float3 projectileSpawnPoint = localTransform.ValueRO.TransformPoint(shootAttack.ValueRO.projectileSpawnPointLocalPosition);
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
        }
    }
}
