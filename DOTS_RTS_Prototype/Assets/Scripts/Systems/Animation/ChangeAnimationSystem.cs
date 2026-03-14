using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[UpdateBefore(typeof(ActiveAnimationSystem))]
partial struct ChangeAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AnimationDataRegistry>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        AnimationDataRegistry animationDataRegistry = SystemAPI.GetSingleton<AnimationDataRegistry>();

        ChangeAnimationJob changeAnimationJob = new ChangeAnimationJob
        {
            animationDataBlobArrayAssetReference = animationDataRegistry.animationDataBlobArrayReference
        };
        changeAnimationJob.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct ChangeAnimationJob : IJobEntity
{
    public BlobAssetReference<BlobArray<AnimationData>> animationDataBlobArrayAssetReference;

    public void Execute(ref ActiveAnimation activeAnimation, ref MaterialMeshInfo materialMeshInfo)
    {
        bool currentIsBusy = false;
        if (activeAnimation.activeAnimationKey != default)
        {
            ref AnimationData currentAnimData = 
                ref RegistryAccessor.GetAnimationData(
                    ref animationDataBlobArrayAssetReference,
                    activeAnimation.activeAnimationKey);

            currentIsBusy = currentAnimData.IsUninterruptible();
        }

        // Skip if current animation is busy
        if (currentIsBusy)
        {
            return;
        }

        if (activeAnimation.activeAnimationKey != activeAnimation.nextAnimationKey || activeAnimation.activeAnimationKey == default)
        {
            //Set new animation
            activeAnimation.currentFrame = 0;
            activeAnimation.framePhaseTime = 0;
            activeAnimation.activeAnimationKey = activeAnimation.nextAnimationKey;

            //Get and set first frame
            ref AnimationData newAnimData =
                ref RegistryAccessor.GetAnimationData(
                    ref animationDataBlobArrayAssetReference,
                    activeAnimation.activeAnimationKey);

            //If there is no animation simply don't animate.
            //Pretty much a workaround for null animations while animations can't be nullable
            if (newAnimData.animationType == AnimationType.None)
            {
                return;
            }

            //Locate inside animationDataRegistry.animationDataBlobArrayAssetReference the animation through its AnimationKey
            materialMeshInfo.Mesh = newAnimData.frameMeshIdIndex[0];
        }
    }
}