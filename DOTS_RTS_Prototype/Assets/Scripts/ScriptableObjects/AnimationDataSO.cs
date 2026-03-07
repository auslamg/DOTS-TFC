using Unity.Collections;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "AnimationDataSO", menuName = "AnimationData/AnimationDataSO")]
public class AnimationDataSO : ScriptableObject
{
    [SerializeField] string animationName;
    [SerializeField] AnimationType animationType;

    [SerializeField, HideInInspector]
    private AnimationKey cachedKey;

    public AnimationKey animationKey => cachedKey;
    public bool playFull;
    public Mesh[] meshArray;
    public float frameFrequency;

    private void OnValidate()
    {
        cachedKey = new AnimationKey
        {
            name = animationName,
            animationType = animationType
        };
    }
}

public struct AnimationKey : IEquatable<AnimationKey>, IComparable<AnimationKey>
{
    public FixedString64Bytes name;
    public AnimationType animationType;
    public bool playFull;

    public bool Equals(AnimationKey other)
    {
        // Only compare fields that define key uniqueness
        return name.Equals(other.name) && animationType == other.animationType;
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
            hash = hash * 23 + animationType.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(AnimationKey left, AnimationKey right) => left.Equals(right);
    public static bool operator !=(AnimationKey left, AnimationKey right) => !left.Equals(right);

    public override string ToString()
    {
        return $"{name}[AnimationType:{animationType}]";
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

public enum AnimationType
{
    None,
    Idle,
    Move,
    Melee,
    Shoot,
    Aim
}