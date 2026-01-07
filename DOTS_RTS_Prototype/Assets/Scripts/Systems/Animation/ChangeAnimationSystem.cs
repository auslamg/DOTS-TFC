using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;

[UpdateBefore(typeof(ActiveAnimationSystem))]
partial struct ChangeAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        AnimationDataHolder animationDataHolder = SystemAPI.GetSingleton<AnimationDataHolder>();
        foreach ((
            RefRW<ActiveAnimation> activeAnimation,
            RefRW<MaterialMeshInfo> materialMeshInfo)
                in SystemAPI.Query<
                RefRW<ActiveAnimation>,
                RefRW<MaterialMeshInfo>>())
        {
            if (activeAnimation.ValueRO.activeAnimationType != activeAnimation.ValueRO.nextAnimationType)
            {
                //Set new animation
                activeAnimation.ValueRW.currentFrame = 0;
                activeAnimation.ValueRW.framePhaseTime = 0;
                activeAnimation.ValueRW.activeAnimationType = activeAnimation.ValueRO.nextAnimationType;

                //Get and set first frame
                ref AnimationData animData =
                                ref animationDataHolder.animationDataBlobArrayAssetReference.Value[(int)activeAnimation.ValueRW.activeAnimationType];
                materialMeshInfo.ValueRW.MeshID = animData.batchMeshIdBlobArray[0];
            }
        }
    }
}
