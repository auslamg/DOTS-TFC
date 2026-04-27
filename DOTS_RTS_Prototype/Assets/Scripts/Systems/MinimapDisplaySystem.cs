using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

partial struct MinimapDisplaySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float iconSize = GetIconSize(ref state);

        foreach (var minimapDisplay in SystemAPI.Query<RefRW<MinimapDisplay>>())
        {
            RefRW<LocalTransform> iconLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(minimapDisplay.ValueRO.minimapIconEntity);
            iconLocalTransform.ValueRW.Scale = iconSize / 32;
            Debug.Log($"Setting icon size {iconLocalTransform.ValueRO.Scale}");
        }
    }

    private float GetIconSize(ref SystemState state)
    {
        return MinimapCameraHandler.Instance.GetIconSizeMultiplier;
    }
}
