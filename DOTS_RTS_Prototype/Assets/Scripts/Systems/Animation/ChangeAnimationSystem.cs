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
        foreach ((
            RefRW<ActiveAnimation> activeAnimation,
            RefRW<MaterialMeshInfo> materialMeshInfo)
                in SystemAPI.Query<
                RefRW<ActiveAnimation>,
                RefRW<MaterialMeshInfo>>())
        {
            //TODO: Refactor into "PlayFull tag" or something
            //If the current animation is PlayFull (shoot), then do NOT change it
            if (activeAnimation.ValueRO.activeAnimationType == AnimationDataSO.AnimationType.SoldierShoot)
            {
                Debug.Log("Busy shooting!");
                continue;
            }
            if (activeAnimation.ValueRO.activeAnimationType == AnimationDataSO.AnimationType.ZombieAttack)
            {
                Debug.Log("Busy shooting!");
                continue;
            }

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
