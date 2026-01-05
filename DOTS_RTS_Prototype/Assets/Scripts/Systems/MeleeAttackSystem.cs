using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

partial struct MeleeAttackSystem : ISystem
{
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
                WithDisabled<MoveOverride>())
        {
            //FIX: Avoid continue. Maybe labels/goto?
            if (!state.EntityManager.ExistsAndPersists(targetter.ValueRO.targetEntity))
            {
                continue;
            }

            //Calculate if the target can be attacked
            LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(targetter.ValueRO.targetEntity);
            float distanceToTarget = math.distance(localTransform.ValueRO.Position, targetLocalTransform.Position);
            bool isWithinAttackDistance = distanceToTarget < meleeAttack.ValueRO.attackDistance;

            //REVIEW: THIS MIGHT CATCH ISSUES WITH BUILDINGS
            //Note:
            //This differs greatly from the tutorial.
            //Real distance is calculated taking into account both target's and attacker's collider radius, obtained at baking Unit component.
            //This is to avoid performance cost of RayCasts, which are a far superior order of magnitude in performance cost scaling.
            //Might come at the inconvenienve of sucking for non-circular colliders, like BoxColliders or horizontal capsules.
            bool isTouchingTarget = false;
            if (!isWithinAttackDistance)
            {
                //Incomplete tutorial code. Requires external declarations
                /* NativeList<RaycastHit> raycastHitList = new NativeList<RaycastHit>(Allocator.Temp);
                float3 dirToTarget = targetLocalTransform.Position - localTransform.ValueRO.Position;
                dirToTarget = math.normalize(dirToTarget);
                float distanceExtraToTestRayCast = .4f;
                RaycastInput raycastInput = new RaycastInput
                {
                    Start = localTransform.ValueRO.Position,
                    End = localTransform.ValueRO.Position + dirToTarget * (unit.ValueRO.colliderOffsetRadius + distanceExtraToTestRayCast),
                    Filter = CollisionFilter.Default
                };
                raycastHitList.Clear(); */

                Unit targetUnit = SystemAPI.GetComponent<Unit>(targetter.ValueRO.targetEntity);
                float minDistanceOffset = 
                    meleeAttack.ValueRO.attackDistance + 
                    targetUnit.colliderOffsetRadius + 
                    unit.ValueRO.colliderOffsetRadius + 
                    unitMover.ValueRO.targetReachedDistanceSquared;
                isTouchingTarget = distanceToTarget < minDistanceOffset;
            }

            if (!isWithinAttackDistance && !isTouchingTarget)
            {
                //Target is too far
                unitMover.ValueRW.targetPosition = targetLocalTransform.Position;
            }
            else
            {
                //Target is close enough to attack
                unitMover.ValueRW.targetPosition = localTransform.ValueRO.Position;

                //TODO: Extract into AttackLoop() method or corroutine
                //IDEA: Refactor into corroutines
                //FIX: Avoid continue. Maybe labels/goto?
                meleeAttack.ValueRW.attackPhaseTime -= SystemAPI.Time.DeltaTime;
                if (meleeAttack.ValueRO.attackPhaseTime > 0)
                {
                    continue;
                }
                meleeAttack.ValueRW.attackPhaseTime = meleeAttack.ValueRO.attackFrequency;

                RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(targetter.ValueRO.targetEntity);
                targetHealth.ValueRW.currentHealth -= meleeAttack.ValueRO.damageAmount;
                targetHealth.ValueRW.onHealthChanged = true;
            }
        }
    }
}
