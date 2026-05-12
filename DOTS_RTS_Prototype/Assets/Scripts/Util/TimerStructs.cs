using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[BurstCompile]
[Serializable]
public struct LoopingTimer
{
    public float Time;
    public float Interval;

    public bool Tick(float delta)
    {
        Time -= delta;
        if (Time <= 0)
        {
            Time = Interval;
            return true;
        }
        else return false;
    }

    public bool IsTicking()
    {
        return Time == Interval;
    }

    public void Reset(bool readyToTick)
    {
        Time = readyToTick ? 0 : Interval;
    }
}

[BurstCompile]
[Serializable]
public struct DynamicTimer
{
    public float Time;
    public bool IsTicking;

    public bool Tick(float delta, float interval)
    {
        Time += delta;
        if (Time >= interval)
        {
            Time = 0;
            IsTicking = true;
            return true;
        }
        else
        {
            IsTicking = false;
            return false;
        }
    }

    public void Reset(bool readyToTick)
    {
        Time = 0;
    }
}

/* [BurstCompile]
[Serializable]
public struct LoopingCounter
{
    public int Step;
    public int Max;

    public int Tick()
    {
        int current = Step;

        Step++;
        if (Step >= Max)
            Step = 0;

        return current;
    }

    public void Reset()
    {
        Step = 0;
    }

    public int Next() => (Step + 1) >= Max ? 0 : (Step + 1);
    public int Previous() => (Step - 1) < 0 ? Max - 1 : (Step - 1);
} */
