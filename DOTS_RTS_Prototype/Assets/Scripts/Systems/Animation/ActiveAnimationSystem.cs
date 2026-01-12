using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

partial struct ActiveAnimationSystem : ISystem
{
    [ReadOnly] public ComponentLookup<Parent> parentComponentLookup;
    [ReadOnly] public ComponentLookup<UnitAnimations> unitAnimationsComponentLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AnimationDataHolder>();
        parentComponentLookup = state.GetComponentLookup<Parent>(true);
        unitAnimationsComponentLookup = state.GetComponentLookup<UnitAnimations>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        parentComponentLookup.Update(ref state);
        unitAnimationsComponentLookup.Update(ref state);
        AnimationDataHolder animationDataHolder = SystemAPI.GetSingleton<AnimationDataHolder>();

        ActiveAnimationJob job = new ActiveAnimationJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            animationDataBlobArrayAssetReference = animationDataHolder.animationDataBlobArrayAssetReference,
            parentComponentLookup = parentComponentLookup,
            unitAnimationsComponentLookup = unitAnimationsComponentLookup
        };
        job.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct ActiveAnimationJob : IJobEntity
{
    public float deltaTime;
    public BlobAssetReference<BlobArray<AnimationData>> animationDataBlobArrayAssetReference;
    [ReadOnly] public ComponentLookup<Parent> parentComponentLookup;
    [ReadOnly] public ComponentLookup<UnitAnimations> unitAnimationsComponentLookup;

    public void Execute(Entity entity, ref ActiveAnimation activeAnimation, ref MaterialMeshInfo materialMeshInfo)
    {
        Debug.Log("Code reached");

        //Cached AnimDataBlobArrayAsset reference index pointer for readability
        ref AnimationData animData =
            ref EntityUtil.GetAnimationData(animationDataBlobArrayAssetReference, activeAnimation.activeAnimationKey);

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

            //Get UnitAnimations inside parent's component
            RefRO<UnitAnimations> unitAnimations = 
                unitAnimationsComponentLookup.GetRefRO(parentComponentLookup[entity].Value);

            //TODO: Refactor into "PlayFull tag" or something
            //Note: Since this runs inside the animation clock, it will only trigger when trying to run the next frame. Therefore the duration of a PlayFull animation equals frameCount*frameFrequency.
            if (activeAnimation.currentFrame == 0 &&
                activeAnimation.activeAnimationKey.animationType == AnimationType.Shoot)
            {
                

                Debug.Log("Turned back to 0!");
                activeAnimation.activeAnimationKey = unitAnimations.ValueRO.noneAnimationKey;
            }
            if (activeAnimation.currentFrame == 0 &&
                activeAnimation.activeAnimationKey.animationType == AnimationType.Melee)
            {
                Debug.Log("Turned back to 0!");
                activeAnimation.activeAnimationKey = unitAnimations.ValueRO.noneAnimationKey;
            }
        }
    }
}

