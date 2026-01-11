using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

partial struct ActiveAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AnimationDataHolder>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        AnimationDataHolder animationDataHolder = SystemAPI.GetSingleton<AnimationDataHolder>();

        ActiveAnimationJob job = new ActiveAnimationJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            animationDataBlobArrayAssetReference = animationDataHolder.animationDataBlobArrayAssetReference
        };
        job.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct ActiveAnimationJob : IJobEntity
{
    public float deltaTime;
    public BlobAssetReference<BlobArray<AnimationData>> animationDataBlobArrayAssetReference;
    public void Execute(ref ActiveAnimation activeAnimation, ref MaterialMeshInfo materialMeshInfo)
    {
        //Cached AnimDataBlobArrayAsset reference index pointer for readability
        ref AnimationData animData =
            ref animationDataBlobArrayAssetReference.Value[(int)activeAnimation.activeAnimationType];

        //Time loop
        //IDEA: Use corroutines
        activeAnimation.framePhaseTime += deltaTime;
        if (activeAnimation.framePhaseTime >= animData.frameFrequency)
        {
            activeAnimation.framePhaseTime -= animData.frameFrequency;


            //Animation loop
            //IDEA: Use corroutines
            activeAnimation.currentFrame += 1;
            if (activeAnimation.currentFrame >= animData.frameCount)
            {
                activeAnimation.currentFrame = 0;
            }

            materialMeshInfo.MeshID =
                animData.batchMeshIdBlobArray[activeAnimation.currentFrame];

            //TODO: Refactor into "PlayFull tag" or something
            //Note: Since this runs inside the animation clock, it will only trigger when trying to run the next frame. Therefore the duration of a PlayFull animation equals frameCount*frameFrequency.
            if (activeAnimation.currentFrame == 0 &&
                activeAnimation.activeAnimationType == AnimationDataSO.AnimationType.SoldierShoot)
            {
                Debug.Log("Turned back to 0!");
                activeAnimation.activeAnimationType = AnimationDataSO.AnimationType.None;
            }
            if (activeAnimation.currentFrame == 0 &&
                activeAnimation.activeAnimationType == AnimationDataSO.AnimationType.ZombieAttack)
            {
                Debug.Log("Turned back to 0!");
                activeAnimation.activeAnimationType = AnimationDataSO.AnimationType.None;
            }
        }
    }
}

