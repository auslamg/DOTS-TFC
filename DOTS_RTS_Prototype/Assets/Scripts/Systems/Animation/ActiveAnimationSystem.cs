using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Advances active animation clips and updates mesh frames on animated entities.
/// </summary>
partial struct ActiveAnimationSystem : ISystem
{
    [ReadOnly] public ComponentLookup<Parent> parentComponentLookup;
    [ReadOnly] public ComponentLookup<UnitAnimations> unitAnimationsComponentLookup;

    /// <summary>
    /// Initializes component lookups required by the animation update job.
    /// </summary>
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AnimationDataRegistry>();
        parentComponentLookup = state.GetComponentLookup<Parent>(true);
        unitAnimationsComponentLookup = state.GetComponentLookup<UnitAnimations>(true);
    }

    /// <summary>
    /// Updates lookup caches and schedules frame progression for all active animations.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        parentComponentLookup.Update(ref state);
        unitAnimationsComponentLookup.Update(ref state);
        AnimationDataRegistry animationDataRegistry = SystemAPI.GetSingleton<AnimationDataRegistry>();

        ActiveAnimationJob job = new ActiveAnimationJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            animationDataBlobArrayAssetReference = animationDataRegistry.animationDataBlobArrayReference,
            parentComponentLookup = parentComponentLookup,
            unitAnimationsComponentLookup = unitAnimationsComponentLookup
        };
        job.ScheduleParallel();
    }
}

[BurstCompile]
/// <summary>
/// Applies per-entity animation timing and writes the current frame mesh to rendering data.
/// </summary>
public partial struct ActiveAnimationJob : IJobEntity
{
    public float deltaTime;
    public BlobAssetReference<BlobArray<AnimationData>> animationDataBlobArrayAssetReference;
    [ReadOnly] public ComponentLookup<Parent> parentComponentLookup;
    [ReadOnly] public ComponentLookup<UnitAnimations> unitAnimationsComponentLookup;

    /// <summary>
    /// Advances frame timers, loops clip playback, and resolves uninterruptible state transitions.
    /// </summary>
    public void Execute(Entity entity, ref ActiveAnimation activeAnimation, ref MaterialMeshInfo materialMeshInfo)
    {
        //Cached AnimDataBlobArrayAsset reference index pointer for readability
        ref AnimationData animData =
            ref DataLookup.GetAnimationData(
                ref animationDataBlobArrayAssetReference,
                activeAnimation.activeAnimationKey);

        //If there is no animation simply don't animate.
        //Pretty much a workaround for null animations while animations can't be nullable
        if (animData.animationType == AnimationType.None)
        {
            Debug.Log("NONE ANIMATION");
            return;
        }

        //Time loop
        activeAnimation.framePhaseTime += deltaTime;
        if (activeAnimation.framePhaseTime >= animData.frameFrequency)
        {
            activeAnimation.framePhaseTime -= animData.frameFrequency;

            //Animation loop
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

            // TODO: Document why exactly this works as opposed to:
            /* (activeAnimation.currentFrame == 0 && animData.IsUninterruptible()) */
            // REVIEW: This might scale weird? Check with new animations
            /// If not doing this, after finishing an uninterruptible animation the first frame of that animation will play for
            /// a single game frame. This is probably because the logic would only run on the frame 0, therefore after the
            /// animation had already started. On why it works well with animData.frameCount - 2, no idea, might just be
            /// a coincidence that the only melee attack animation matches the idle animation pose loosely and this wasn't solved
            if ((activeAnimation.currentFrame == animData.frameCount - 2 ||
                    (activeAnimation.currentFrame == 0 &&
                    animData.frameCount < 2)) &&
                animData.IsUninterruptible())
            {
                //Busy attacking
                activeAnimation.activeAnimationKey = unitAnimations.ValueRO.noneAnimationKey;
            }
        }
    }
}

