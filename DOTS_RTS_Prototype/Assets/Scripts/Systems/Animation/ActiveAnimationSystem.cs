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
        state.RequireForUpdate<AnimationDataRegistry>();
        parentComponentLookup = state.GetComponentLookup<Parent>(true);
        unitAnimationsComponentLookup = state.GetComponentLookup<UnitAnimations>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        parentComponentLookup.Update(ref state);
        unitAnimationsComponentLookup.Update(ref state);
        AnimationDataRegistry animationDataHolder = SystemAPI.GetSingleton<AnimationDataRegistry>();

        ActiveAnimationJob job = new ActiveAnimationJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            animationDataBlobArrayAssetReference = animationDataHolder.animationDataBlobArrayReference,
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
        //If there is no animation simply don't animate.
        //Pretty much a workaround for null animations while animations can't be nullable
        if (activeAnimation.activeAnimationKey.animationType == AnimationType.None)
        {
            return;
        }

        //Cached AnimDataBlobArrayAsset reference index pointer for readability
        ref AnimationData animData =
            ref RegistryAccessor.GetAnimationData(
                ref animationDataBlobArrayAssetReference,
                activeAnimation.activeAnimationKey);

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

            materialMeshInfo.Mesh =
                animData.frameMeshIdIndex[activeAnimation.currentFrame];

            //Get UnitAnimations inside parent's component
            RefRO<UnitAnimations> unitAnimations =
                unitAnimationsComponentLookup.GetRefRO(parentComponentLookup[entity].Value);

            //TODO: Refactor into "PlayFull tag" or something
            //Note: Since this runs inside the animation clock, it will only trigger when trying to run the next frame. Therefore the duration of a PlayFull animation equals frameCount*frameFrequency.
            if (activeAnimation.currentFrame == 0 &&
                activeAnimation.activeAnimationKey.IsUninterruptible())
            {
                //Busy attacking
                activeAnimation.activeAnimationKey = unitAnimations.ValueRO.noneAnimationKey;
            }
        }
    }
}

