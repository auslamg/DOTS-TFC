using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Updates minimap icon scales based on camera zoom level.
/// </summary>
partial struct MinimapDisplaySystem : ISystem
{
    private float previousValue;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float iconSize = GetIconSize(ref state);

        if (iconSize != previousValue)
        {
            foreach (var minimapDisplay in SystemAPI.Query<RefRW<MinimapDisplay>>())
            {
                RefRW<LocalTransform> iconLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(minimapDisplay.ValueRO.minimapIconEntity);
                iconLocalTransform.ValueRW.Scale = iconSize / 32;
                /* Debug.Log($"Setting icon size {iconLocalTransform.ValueRO.Scale}"); */
            }
            previousValue = iconSize;
        }
    }

    private float GetIconSize(ref SystemState state)
    {
        return MinimapCameraHandler.Instance.GetIconSizeMultiplier;
    }
}
