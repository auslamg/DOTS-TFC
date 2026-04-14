using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Utility class for entities and their components.
/// </summary>
public static class StructUtil
{

}

[BurstCompile]
public struct LoopCounter
{
    public int Value;
    public int Max;

    public int Tick()
    {
        int current = Value;

        Value++;
        if (Value >= Max)
            Value = 0;

        return current;
    }

    public int Next() => (Value + 1) >= Max ? 0 : (Value + 1);
    public int Previous() => (Value - 1) < 0 ? Max - 1 : (Value - 1);
}
