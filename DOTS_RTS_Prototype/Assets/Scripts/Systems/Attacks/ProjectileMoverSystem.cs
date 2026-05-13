using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Moves active projectile entities toward their current targets and resolves impact effects.
/// </summary>
partial struct ProjectileMoverSystem : ISystem
{
    /// <summary>
    /// Updates projectile movement, impact detection, and impact side effects.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((
            RefRW<LocalTransform> localTransform,
            RefRO<Projectile> projectile,
            RefRO<Targetter> targetter,
            Entity entity)
                in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRO<Projectile>,
                RefRO<Targetter>>().
                WithEntityAccess())
        {
            //If there is no target, destroy this and go for next entity
            if (!EntityUtil.ExistsAndPersists(ref state, targetter.ValueRO.targetEntity))
            {
                entityCommandBuffer.DestroyEntity(entity);
            }
            else
            {
                // Resolve target hit point: transform shootable offset from local to world space
                LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(targetter.ValueRO.targetEntity);
                Shootable targetShootable = SystemAPI.GetComponent<Shootable>(targetter.ValueRO.targetEntity);
                float3 targetPosition = targetLocalTransform.TransformPoint(targetShootable.hitPointPosition);

                // Position previous to moving, used for calculating if the projectile overshot the target
                float distanceBeforeMovingSquared = math.distancesq(localTransform.ValueRO.Position, targetPosition);

                float3 moveDirection = targetPosition - localTransform.ValueRO.Position;
                moveDirection = math.normalize(moveDirection);

                localTransform.ValueRW.Position += moveDirection * projectile.ValueRO.speed * SystemAPI.Time.DeltaTime;
                localTransform.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());

                //Position after moving, used for calculating if the projectile overshot the target
                float distanceAfterMovingSquared = math.distancesq(localTransform.ValueRO.Position, targetPosition);

                //Target overshoot countermeasure.
                if (distanceBeforeMovingSquared < distanceAfterMovingSquared)
                {
                    localTransform.ValueRW.Position = targetPosition;
                }

                // Destroy projectile and apply effects when close enough to target.
                float destroyDistanceSquared = .2f;
                if (math.distancesq(localTransform.ValueRO.Position, targetPosition) <= destroyDistanceSquared)
                {
                    // Damage target.
                    RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(targetter.ValueRO.targetEntity);
                    targetHealth.ValueRW.currentHealth -= projectile.ValueRO.damageAmount;
                    targetHealth.ValueRW.onHealthChanged = true;

                    // If the target has no target themselves set it to the shooter for retribution.
                    if (SystemAPI.HasComponent<Targetter>(targetter.ValueRO.targetEntity))
                    {
                        RefRW<Targetter> targetOwnTargetter = SystemAPI.GetComponentRW<Targetter>(targetter.ValueRO.targetEntity);
                        if (!EntityUtil.ExistsAndPersists(ref state, targetOwnTargetter.ValueRO.targetEntity))
                        {
                            targetOwnTargetter.ValueRW.targetEntity = projectile.ValueRO.shooterEntity;
                        }
                    }

                    entityCommandBuffer.DestroyEntity(entity);
                }
            }
        }
    }
}
