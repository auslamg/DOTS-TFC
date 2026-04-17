using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

/// <summary>
/// Handles melee target chasing and timed melee damage application.
/// </summary>
partial struct MeleeAttackSystem : ISystem
{
    /// <summary>
    /// Moves units into melee range and applies damage when attack cooldowns expire.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<MeleeAttack> meleeAttack,
            RefRO<Targetter> targetter,
            RefRO<UnitMover> unitMover,
            RefRW<PathRequest> pathRequest,
            EnabledRefRW<PathRequest> pathRequestEnabled,
            RefRO<Unit> unit)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<MeleeAttack>,
                RefRO<Targetter>,
                RefRO<UnitMover>,
                RefRW<PathRequest>,
                EnabledRefRW<PathRequest>,
                RefRO<Unit>>().
                WithDisabled<ManualMove>().
                WithPresent<PathRequest>())
        {
            if (EntityUtil.ExistsAndPersists(ref state, targetter.ValueRO.targetEntity))
            {
                //Calculate if the target can be attacked
                LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(targetter.ValueRO.targetEntity);
                float distanceToTarget = math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position);
                bool isWithinAttackDistance = distanceToTarget < meleeAttack.ValueRO.attackDistance;

                //REVIEW: THIS MIGHT CATCH ISSUES WITH BUILDING ATTACKS
                bool isTouchingTarget = false;
                if (!isWithinAttackDistance)
                {
                    if (SystemAPI.HasComponent<Unit>(targetter.ValueRO.targetEntity))
                    {
                        Unit targetUnit = SystemAPI.GetComponent<Unit>(targetter.ValueRO.targetEntity);
                        float minDistanceOffset =
                            meleeAttack.ValueRO.attackDistance +
                            targetUnit.colliderOffsetRadius +
                            unit.ValueRO.colliderOffsetRadius +
                            unitMover.ValueRO.targetReachedDistanceSquared;
                        isTouchingTarget = distanceToTarget < minDistanceOffset;
                    }
                    else if (SystemAPI.HasComponent<Building>(targetter.ValueRO.targetEntity))
                    {
                        Building targetBuilding = SystemAPI.GetComponent<Building>(targetter.ValueRO.targetEntity);
                        float minDistanceOffset =
                            meleeAttack.ValueRO.attackDistance +
                            targetBuilding.colliderOffsetRadius +
                            unit.ValueRO.colliderOffsetRadius +
                            unitMover.ValueRO.targetReachedDistanceSquared;
                        isTouchingTarget = distanceToTarget < minDistanceOffset;
                    }
                }

                if (!isWithinAttackDistance && !isTouchingTarget)
                {
                    //Target is too far to attack
                    pathRequest.ValueRW.targetPosition = targetLocalTransform.Position;
                    pathRequestEnabled.ValueRW = true;
                }
                else
                {
                    //Target is close enough to attack
                    pathRequest.ValueRW.targetPosition = localTransform.ValueRO.Position;
                    pathRequestEnabled.ValueRW = true;

                    // Attack cooldown timer
                    ref LoopingTimer attackTimer = ref meleeAttack.ValueRW.attackTimer;
                    /* attackTimer.ClampUpdate(SystemAPI.Time.DeltaTime); */
                    if (attackTimer.Tick(SystemAPI.Time.DeltaTime))
                    {
                        //Damage target
                        RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(targetter.ValueRO.targetEntity);
                        targetHealth.ValueRW.currentHealth -= meleeAttack.ValueRO.damageAmount;
                        targetHealth.ValueRW.onHealthChanged = true;

                        meleeAttack.ValueRW.onAttack = true;
                    }
                }
            }
        }
    }
}
