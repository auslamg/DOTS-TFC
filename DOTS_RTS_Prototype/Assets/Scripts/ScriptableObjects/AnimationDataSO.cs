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

public struct AnimationKey : IEquatable<AnimationKey>
{
    public FixedString64Bytes name;
    public AnimationType animationType;
    public bool playFull;
    public override bool Equals(object obj)
    {
        if (!(obj is AnimationKey))
            return false;

        AnimationKey other = (AnimationKey)obj;
        return this.name == other.name;
    }

    public bool Equals(AnimationKey other)
    {
        return name.Equals(other.name);
    }

    public override string ToString()
    {
        return name + "[AnimationType:" + animationType + "]";
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

    public static bool operator ==(AnimationKey key1, AnimationKey key2)
    {
        return key1.Equals(key2);
    }

    public static bool operator !=(AnimationKey key1, AnimationKey key2)
    {
        return !key1.Equals(key2);
    }

    public bool IsUninterruptible()
    {
        if (playFull) return playFull;
        
        switch (animationType)
        {
            default:
            case AnimationType.None:
            case AnimationType.Idle:
            case AnimationType.Move:
            case AnimationType.Aim:
                return false;
            case AnimationType.Melee:
            case AnimationType.Shoot:
                return true;
        }
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