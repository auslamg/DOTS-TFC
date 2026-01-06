using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

partial struct ActiveAnimationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
           RefRW<ActiveAnimation> activeAnimation,
           RefRW<MaterialMeshInfo> materialMeshInfo)
               in SystemAPI.Query<
               RefRW<ActiveAnimation>,
               RefRW<MaterialMeshInfo>>())
        {
            //Time loop
            activeAnimation.ValueRW.framePhaseTime += SystemAPI.Time.DeltaTime;
            if (activeAnimation.ValueRO.framePhaseTime >= activeAnimation.ValueRO.frameFrequency)
            {
                activeAnimation.ValueRW.framePhaseTime -= activeAnimation.ValueRO.frameFrequency;

                Debug.Log("Changed animation");

                //Animation loop
                activeAnimation.ValueRW.currentFrame += 1;
                if (activeAnimation.ValueRO.currentFrame >= activeAnimation.ValueRO.frameCount)
                {
                    activeAnimation.ValueRW.currentFrame = 0;
                }
                
                Debug.Log("Changed animation");

                switch (activeAnimation.ValueRO.currentFrame)
                {
                    default:
                    case 0:
                        materialMeshInfo.ValueRW.MeshID = activeAnimation.ValueRO.frame0;
                        break;
                    case 1:
                        materialMeshInfo.ValueRW.MeshID = activeAnimation.ValueRO.frame1;
                        break;
                }
            }
        }
    }
}
