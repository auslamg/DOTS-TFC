using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
[UpdateAfter(typeof(ShootAttackSystem))]
partial struct AnimationStateSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRO<AnimatedMeshReference> animatedMesh,
            RefRO<UnitMover> unitMover,
            RefRO<UnitAnimations> unitAnimations)
                in SystemAPI.Query<
                RefRO<AnimatedMeshReference>,
                RefRO<UnitMover>,
                RefRO<UnitAnimations>>())
        {
            RefRW<ActiveAnimation> activeAnimation =
                SystemAPI.GetComponentRW<ActiveAnimation>(animatedMesh.ValueRO.meshEntity);

            //TODO: Refactor into fsm
            if (unitMover.ValueRO.isMoving)
            {
                activeAnimation.ValueRW.nextAnimationType = unitAnimations.ValueRO.walkAnimationType;
            }
            else
            {
                activeAnimation.ValueRW.nextAnimationType = unitAnimations.ValueRO.idleAnimationType;
            }
        }

        foreach ((
            RefRO<AnimatedMeshReference> animatedMesh,
            RefRO<ShootAttack> shootAttack,
            RefRO<UnitMover> unitMover,
            RefRO<Targetter> targetter,
            RefRO<UnitAnimations> unitAnimations)
                in SystemAPI.Query<
                RefRO<AnimatedMeshReference>,
                RefRO<ShootAttack>,
                RefRO<UnitMover>,
                RefRO<Targetter>,
                RefRO <UnitAnimations>>())
        {

            //Explanation
            //If the entity is NOT moving AND has a target, default to aim.
            //If it additionally is onShoot TRUE, then shoot

            //The animation will NOT get changed if it's currently a PlayFull (shoot) animation
            //Playfull animations will default to None after playing once, meaning other animations
            //can finally play.
            
            //FIX: Use utils method
            //TODO: Refactor into fsm
            if (!unitMover.ValueRO.isMoving && targetter.ValueRO.targetEntity != Entity.Null)
            {
                RefRW<ActiveAnimation> activeAnimation =
                    SystemAPI.GetComponentRW<ActiveAnimation>(animatedMesh.ValueRO.meshEntity);
                activeAnimation.ValueRW.nextAnimationType = unitAnimations.ValueRO.aimAnimationType;
            }


            //TODO: Refactor into fsm
            if (shootAttack.ValueRO.onShoot.isTriggered)
            {
                RefRW<ActiveAnimation> activeAnimation =
                    SystemAPI.GetComponentRW<ActiveAnimation>(animatedMesh.ValueRO.meshEntity);
                activeAnimation.ValueRW.nextAnimationType = unitAnimations.ValueRO.shootAnimationType;
            }
        }

        foreach ((
            RefRO<AnimatedMeshReference> animatedMesh,
            RefRO<MeleeAttack> meleeAttack,
            RefRO<UnitAnimations> unitAnimations)
                in SystemAPI.Query<
                RefRO<AnimatedMeshReference>,
                RefRO<MeleeAttack>,
                RefRO<UnitAnimations>>())
        {
            
            //FIX: Use utils method
            //TODO: Refactor into fsm

            /* if (!unitMover.ValueRO.isMoving && targetter.ValueRO.targetEntity != Entity.Null)
            {
                RefRW<ActiveAnimation> activeAnimation =
                    SystemAPI.GetComponentRW<ActiveAnimation>(animatedMesh.ValueRO.meshEntity);
                activeAnimation.ValueRW.nextAnimationType = unitAnimations.ValueRO.aimAnimationType;
            } */


            //TODO: Refactor into fsm
            if (meleeAttack.ValueRO.onAttack)
            {
                RefRW<ActiveAnimation> activeAnimation =
                    SystemAPI.GetComponentRW<ActiveAnimation>(animatedMesh.ValueRO.meshEntity);
                activeAnimation.ValueRW.nextAnimationType = unitAnimations.ValueRO.meleeAttackAnimationType;
            }
        }
    }
}
