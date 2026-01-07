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
        foreach ((
           RefRW<ActiveAnimation> activeAnimation,
           RefRW<MaterialMeshInfo> materialMeshInfo)
               in SystemAPI.Query<
               RefRW<ActiveAnimation>,
               RefRW<MaterialMeshInfo>>())
        {
            //TEST
            if (Input.GetKeyDown(KeyCode.T))
            {
                activeAnimation.ValueRW.nextAnimationType = AnimationDataSO.AnimationType.SoldierIdle;
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                activeAnimation.ValueRW.nextAnimationType = AnimationDataSO.AnimationType.SoldierWalk;
            }

            //Cached AnimDataBlobArrayAsset reference index pointer for readability
            ref AnimationData animData =
                ref animationDataHolder.animationDataBlobArrayAssetReference.Value[(int)activeAnimation.ValueRW.activeAnimationType];

            //Time loop
            //IDEA: Use corroutines
            activeAnimation.ValueRW.framePhaseTime += SystemAPI.Time.DeltaTime;
            if (activeAnimation.ValueRO.framePhaseTime >= animData.frameFrequency)
            {
                activeAnimation.ValueRW.framePhaseTime -= animData.frameFrequency;


                //Animation loop
                //IDEA: Use corroutines
                activeAnimation.ValueRW.currentFrame += 1;
                if (activeAnimation.ValueRO.currentFrame >= animData.frameCount)
                {
                    activeAnimation.ValueRW.currentFrame = 0;
                }

                materialMeshInfo.ValueRW.MeshID =
                    animData.batchMeshIdBlobArray[activeAnimation.ValueRO.currentFrame];
            }
        }
    }
}
