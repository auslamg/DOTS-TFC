using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[UpdateBefore(typeof(ActiveAnimationSystem))]
partial struct ChangeAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        AnimationDataHolder animationDataHolder = SystemAPI.GetSingleton<AnimationDataHolder>();

        ChangeAnimationJob changeAnimationJob = new ChangeAnimationJob
        {
            animationDataBlobArrayAssetReference = animationDataHolder.animationDataBlobArrayAssetReference
        };
        changeAnimationJob.ScheduleParallel();
    }
}

public partial struct ChangeAnimationJob : IJobEntity
{
    public BlobAssetReference<BlobArray<AnimationData>> animationDataBlobArrayAssetReference;

    public void Execute(ref ActiveAnimation activeAnimation, ref MaterialMeshInfo materialMeshInfo)
    {
        //TODO: Refactor into "PlayFull tag" or something
        //If the current animation is PlayFull (shoot), then do NOT change it
        if (activeAnimation.activeAnimationType == AnimationDataSO.AnimationType.SoldierShoot)
        {
            Debug.Log("Busy shooting!");
            return;
        }
        if (activeAnimation.activeAnimationType == AnimationDataSO.AnimationType.ZombieAttack)
        {
            Debug.Log("Busy shooting!");
            return;
        }

        if (activeAnimation.activeAnimationType != activeAnimation.nextAnimationType)
        {
            //Set new animation
            activeAnimation.currentFrame = 0;
            activeAnimation.framePhaseTime = 0;
            activeAnimation.activeAnimationType = activeAnimation.nextAnimationType;

            //Get and set first frame
            ref AnimationData animData =
                            ref animationDataBlobArrayAssetReference.Value[(int)activeAnimation.activeAnimationType];
            materialMeshInfo.MeshID = animData.batchMeshIdBlobArray[0];
        }
    }
}