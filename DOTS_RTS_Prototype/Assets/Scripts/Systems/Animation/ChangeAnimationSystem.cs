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
        AnimationDataRegistry animationDataHolder = SystemAPI.GetSingleton<AnimationDataRegistry>();

        ChangeAnimationJob changeAnimationJob = new ChangeAnimationJob
        {
            animationDataBlobArrayAssetReference = animationDataHolder.animationDataBlobArrayReference
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
        //TODO: Refactor into "PlayFull tag" or something
        //If the current animation is PlayFull (shoot), then do NOT change it
        if (activeAnimation.activeAnimationKey.IsUninterruptible())
        {
            //Busy
            return;
        }

        if (activeAnimation.activeAnimationKey != activeAnimation.nextAnimationKey || activeAnimation.activeAnimationKey == default)
        {
            //Set new animation
            activeAnimation.currentFrame = 0;
            activeAnimation.framePhaseTime = 0;
            activeAnimation.activeAnimationKey = activeAnimation.nextAnimationKey;

            //If there is no animation simply don't animate.
            //Pretty much a workaround for null animations while animations can't be nullable
            if (activeAnimation.activeAnimationKey.animationType == AnimationType.None)
            {
                return;
            }

            //Get and set first frame
            ref AnimationData animData =
            ref RegistryAccessor.GetAnimationData(
                ref animationDataBlobArrayAssetReference,
                activeAnimation.activeAnimationKey);


            //Locate inside animationDataHolder.animationDataBlobArrayAssetReference the animation through its AnimationKey


            materialMeshInfo.Mesh = animData.frameMeshIdIndex[0];
        }
    }
}