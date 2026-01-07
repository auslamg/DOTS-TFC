using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

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
    }
}
