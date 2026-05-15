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
    /// <summary>
    /// The remaining time before the next tick occurs.
    /// </summary>
    public float Time;

    /// <summary>
    /// The interval at which the timer ticks.
    /// </summary>
    public float Interval;

    /// <summary>
    /// Advances the timer by the given delta and returns true when the timer completes its interval.
    /// </summary>
    /// <param name="delta">The time delta to subtract from the timer.</param>
    /// <returns>True when the timer has reached zero and reset; otherwise false.</returns>
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

    /// <summary>
    /// Determines whether the timer is currently waiting for its next tick.
    /// </summary>
    /// <returns>True when the timer is at its interval value and has not yet started counting down.</returns>
    public bool IsTicking()
    {
        return Time == Interval;
    }

    /// <summary>
    /// Resets the timer to either be ready to tick immediately or wait for the full interval.
    /// </summary>
    /// <param name="readyToTick">When true, resets the timer so it will tick on the next update.</param>
    public void Reset(bool readyToTick)
    {
        Time = readyToTick ? 0 : Interval;
    }
}

[BurstCompile]
[Serializable]
public struct DynamicTimer
{
    /// <summary>
    /// The accumulated time since the last tick.
    /// </summary>
    public float Time;

    /// <summary>
    /// Indicates whether the timer has reached the interval in the last update.
    /// </summary>
    public bool IsTicking;

    /// <summary>
    /// Advances the timer by the given delta and evaluates whether the interval has been reached.
    /// </summary>
    /// <param name="delta">The time delta to add to the timer.</param>
    /// <param name="interval">The duration required before the timer ticks.</param>
    /// <returns>True when the timer reaches or exceeds the interval; otherwise false.</returns>
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

    /// <summary>
    /// Resets the timer and clears any accumulated time.
    /// </summary>
    /// <param name="readyToTick">Unused; included for API compatibility.</param>
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
