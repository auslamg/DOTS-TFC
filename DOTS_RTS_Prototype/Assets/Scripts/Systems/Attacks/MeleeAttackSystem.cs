using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Handles melee target chasing and timed melee damage application.
/// </summary>
[UpdateBefore(typeof(UnitMoverSystem))]
partial struct MeleeAttackSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridData>();
    }
    /// <summary>
    /// Moves units into melee range and applies damage when attack cooldowns expire.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRW<LocalTransform> localTransform,
            RefRW<MeleeAttack> meleeAttack,
            RefRW<Targetter> targetter,
            RefRO<UnitMover> unitMover,
            RefRW<PathRequest> pathRequest,
            EnabledRefRW<PathRequest> pathRequestEnabled,
            RefRO<Unit> unit,
            Entity entity)
                in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRW<MeleeAttack>,
                RefRW<Targetter>,
                RefRO<UnitMover>,
                RefRW<PathRequest>,
                EnabledRefRW<PathRequest>,
                RefRO<Unit>>().
                WithDisabled<ManualMove>().
                WithPresent<PathRequest>().
                WithEntityAccess())
        {
            if (EntityUtil.ExistsAndPersists(ref state, targetter.ValueRO.targetEntity))
            {
                //Calculate if the target can be attacked
                LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(targetter.ValueRO.targetEntity);
                float distanceToTarget = math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position);
                bool isWithinAttackDistance = distanceToTarget < meleeAttack.ValueRO.attackDistance;

                //REVIEW: THIS MIGHT CATCH ISSUES WITH BUILDING ATTACKS
                // Check if the sum of this unit plus its target's offset is enough for the melee attack to happen
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
                            math.sqrt(unitMover.ValueRO.targetReachedDistanceSquared);
                        isTouchingTarget = distanceToTarget < minDistanceOffset;
                    }
                    else if (SystemAPI.HasComponent<Building>(targetter.ValueRO.targetEntity))
                    {
                        Building targetBuilding = SystemAPI.GetComponent<Building>(targetter.ValueRO.targetEntity);
                        float minDistanceOffset =
                            meleeAttack.ValueRO.attackDistance +
                            targetBuilding.colliderOffsetRadius +
                            unit.ValueRO.colliderOffsetRadius +
                            math.sqrt(unitMover.ValueRO.targetReachedDistanceSquared);
                        isTouchingTarget = distanceToTarget < minDistanceOffset;
                    }
                }
                if (!isWithinAttackDistance && !isTouchingTarget)
                {
                    // Target is too far to attack. Move to target if not currently moving.
                    if (!unitMover.ValueRO.isMoving)
                    {
                        // Default target position (normalized for unit wonky vertical jitter).
                        float3 targetPosition = targetLocalTransform.Position * new float3(1, 0, 1);

                        GridData gridData = SystemAPI.GetSingleton<GridData>();
                        int2 targetCoords = GridUtil.WorldPositionToCoords(targetLocalTransform.Position, gridData.gridCellSize);

                        // If the target position is inside an obstructed cell & its a building.
                        if (GridUtil.IsObstructed(targetCoords, gridData) &&
                            SystemAPI.HasComponent<Building>(targetter.ValueRO.targetEntity))
                        {
                            // Get the building's occlusion size to optimize nearest cell search.
                            Occluder occluder = SystemAPI.GetComponent<Occluder>(targetter.ValueRO.targetEntity);

                            // If a nearest cell can be reached navigate to it
                            if (GridUtil.TryGetNearestNeighbouringCell(
                                    targetCoords,
                                    occluder.occlusionFootprint.x,
                                    gridData,
                                    out var result))
                            {
                                targetPosition = GridUtil.CoordsToWorldPositionCenter(result, gridData.gridCellSize);
                            }
                            else targetter.ValueRW.targetEntity = Entity.Null;
                        }

                        pathRequest.ValueRW.targetPosition = targetPosition;
                        pathRequest.ValueRW.postFormationPosition = targetPosition;
                        pathRequestEnabled.ValueRW = true;
                        /* Debug.Log("[Melee] Attacking path request"); */
                    }
                }
                else
                {
                    //Target is close enough to attack: stop moving.
                    pathRequest.ValueRW.targetPosition = localTransform.ValueRO.Position;
                    pathRequest.ValueRW.postFormationPosition = localTransform.ValueRO.Position;
                    pathRequestEnabled.ValueRW = true;

                    // Attack cooldown timer.
                    if (meleeAttack.ValueRW.attackCooldownTimer.Tick(SystemAPI.Time.DeltaTime))
                    {
                        // Rotate towards target when attacking.
                        float3 aimDirection = targetLocalTransform.Position - localTransform.ValueRO.Position;
                        aimDirection = math.normalize(aimDirection);
                        quaternion aimRotation = quaternion.LookRotation(aimDirection, math.up());
                        localTransform.ValueRW.Rotation = aimRotation;

                        // Damage target.
                        RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(targetter.ValueRO.targetEntity);
                        targetHealth.ValueRW.currentHealth -= meleeAttack.ValueRO.damageAmount;
                        targetHealth.ValueRW.onHealthChanged = true;

                        meleeAttack.ValueRW.onAttack = true;

                        // If the target has no target themselves set it to the attacker for retribution.
                        if (SystemAPI.HasComponent<Targetter>(targetter.ValueRO.targetEntity))
                        {
                            RefRW<Targetter> targetOwnTargetter = SystemAPI.GetComponentRW<Targetter>(targetter.ValueRO.targetEntity);
                            if (!EntityUtil.ExistsAndPersists(ref state, targetOwnTargetter.ValueRO.targetEntity))
                            {
                                targetOwnTargetter.ValueRW.targetEntity = entity;
                            }
                        }
                    }
                }
            }
        }
    }
}
