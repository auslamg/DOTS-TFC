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
            RefRW<UnitMover> unitMover,
            RefRO<Unit> unit)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<MeleeAttack>,
                RefRO<Targetter>,
                RefRW<UnitMover>,
                RefRO<Unit>>().
                WithDisabled<ManualMove>())
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
                    unitMover.ValueRW.targetPosition = targetLocalTransform.Position;
                }
                else
                {
                    //Target is close enough to attack
                    unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;

                    //TODO: Extract into AttackLoop() method or corroutine
                    // Attack cooldown timer
                    meleeAttack.ValueRW.attackPhaseTime -= SystemAPI.Time.DeltaTime;
                    if (meleeAttack.ValueRO.attackPhaseTime <= 0)
                    {
                        meleeAttack.ValueRW.attackPhaseTime = meleeAttack.ValueRO.attackFrequency;

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
