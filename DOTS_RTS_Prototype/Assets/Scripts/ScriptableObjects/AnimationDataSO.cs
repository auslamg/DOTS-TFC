using Unity.Collections;
using UnityEngine;
using System;

/// <summary>
/// ScriptableObject describing one animation clip represented as an array of baked mesh frames.
/// </summary>
[CreateAssetMenu(fileName = "AnimationDataSO", menuName = "AnimationData/AnimationDataSO")]
public class AnimationDataSO : ScriptableObject
{
    /// <summary>
    /// Category of animation behavior.
    /// </summary>
    [SerializeField]
    [Tooltip("Category of this animation (Idle, Move, Melee, Shoot, etc.).")]
    public AnimationType animationType;

    /// <summary>
    /// Forces the animation to play fully before transitioning.
    /// </summary>
    [SerializeField]
    [Tooltip("If enabled, this animation must complete before being interrupted.")]
    public bool playFull;

    /// <summary>
    /// Time interval between frame swaps.
    /// </summary>
    [SerializeField]
    [Tooltip("Time interval between frame changes for this animation.")]
    public float frameFrequency;

    /// <summary>
    /// Baked mesh frames used for playback.
    /// </summary>
    [SerializeField]
    [Tooltip("Baked mesh frames used to play this animation.")]
    public Mesh[] meshArray;

    [SerializeField, HideInInspector]
    private AnimationKey cachedKey;

    /// <summary>
    /// Deterministic key generated from the asset name.
    /// </summary>
    public AnimationKey animationKey => cachedKey;

    /// <summary>
    /// Refreshes cached key data whenever the asset is modified in the editor.
    /// </summary>
    private void OnValidate()
    {
        cachedKey = new AnimationKey
        {
            name = this.name
        };
    }

    /// <summary>
    /// Returns whether this animation should be treated as uninterruptible.
    /// </summary>
    /// <returns><see langword="true"/> when the animation must fully play before interruption.</returns>
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
    /// <summary>
    /// Fixed-string key value.
    /// </summary>
    public FixedString64Bytes name;

    /// <summary>
    /// Compares two keys for equality.
    /// </summary>
    public bool Equals(AnimationKey other)
    {
        return name.Equals(other.name);
    }

    /// <summary>
    /// Compares this key to another object for equality.
    /// </summary>
    public override bool Equals(object obj)
    {
        return obj is AnimationKey other && Equals(other);
    }

    /// <summary>
    /// Compares this key with another key for sorting.
    /// </summary>
    public int CompareTo(AnimationKey other)
    {
        int cmp = name.CompareTo(other.name);
        return cmp;
    }

    /// <summary>
    /// Returns hash code for dictionary/set usage.
    /// </summary>
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
    /// <summary>
    /// Returns string representation of this key.
    /// </summary>
    public override string ToString()
    {
        return $"{name}";
    }
}

/// <summary>
/// Supported animation categories.
/// </summary>
public enum AnimationType
{
    None,
    Idle,
    Move,
    Melee,
    Shoot,
    Aim
}