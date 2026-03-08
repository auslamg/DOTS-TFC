using Unity.Collections;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "AnimationDataSO", menuName = "AnimationData/AnimationDataSO")]
public class AnimationDataSO : ScriptableObject
{
    public AnimationType animationType;
    public bool playFull;
    public float frameFrequency;
    public Mesh[] meshArray;

    [SerializeField, HideInInspector]
    private AnimationKey cachedKey;
    public AnimationKey animationKey => cachedKey;

    private void OnValidate()
    {
        cachedKey = new AnimationKey
        {
            name = this.name
        };
    }

    public bool IsUninterruptible()
    {
        if (playFull) return true;
        return animationType switch
        {
            AnimationType.Melee => true,
            AnimationType.Shoot => true,
            _ => false
        };
    }
}

/// <summary>
/// Unique identifier for a <see cref="AnimationData"/> struct, obtained from the SO name.
/// </summary>
public struct AnimationKey : IEquatable<AnimationKey>, IComparable<AnimationKey>
{
    public FixedString64Bytes name;
    public bool Equals(AnimationKey other)
    {
        return name.Equals(other.name);
    }
    public override bool Equals(object obj)
    {
        return obj is AnimationKey other && Equals(other);
    }
    public int CompareTo(AnimationKey other)
    {
        int cmp = name.CompareTo(other.name);
        return cmp;
    }
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + name.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(AnimationKey left, AnimationKey right) => left.Equals(right);
    public static bool operator !=(AnimationKey left, AnimationKey right) => !left.Equals(right);
    public override string ToString()
    {
        return $"{name}";
    }
}

public enum AnimationType
{
    None,
    Idle,
    Move,
    Melee,
    Shoot,
    Aim
}