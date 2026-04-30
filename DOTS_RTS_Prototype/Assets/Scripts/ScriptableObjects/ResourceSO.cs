using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName = "ResourceDataSO", menuName = "Resources/Resource")]
public class ResourceSO : ScriptableObject
{
    /// <summary>
    /// Category of this resource.
    /// </summary>
    [SerializeField]
    [Tooltip("Category/type of this resource.")]
    public ResourceType resourceType;

    /// <summary>
    /// Card sprite used by UI lists and buttons.
    /// </summary>
    [SerializeField]
    [Tooltip("Sprite shown for this resource in UI cards/buttons.")]
    public Sprite icon;

    [SerializeField, HideInInspector]
    private ResourceKey cachedKey;

    /// <summary>
    /// Deterministic key generated from the asset name.
    /// </summary>
    public ResourceKey resourceKey => cachedKey;

    /// <summary>
    /// Refreshes cached key data whenever the asset is modified in the editor.
    /// </summary>
    private void OnValidate()
    {
        cachedKey = new ResourceKey
        {
            name = this.name
        };
    }
}

/// <summary>
/// Unique identifier for a <see cref="ResourceData"/> struct, obtained from the SO name.
/// </summary>
[Serializable]
public struct ResourceKey : IEquatable<ResourceKey>, IComparable<ResourceKey>
{
    /// <summary>
    /// Fixed-string key value.
    /// </summary>
    public FixedString64Bytes name;

    /// <summary>
    /// Compares two keys for equality.
    /// </summary>
    public bool Equals(ResourceKey other)
    {
        return name.Equals(other.name);
    }

    /// <summary>
    /// Compares this key to another object for equality.
    /// </summary>
    public override bool Equals(object obj)
    {
        return obj is ResourceKey other && Equals(other);
    }

    /// <summary>
    /// Compares this key with another key for sorting.
    /// </summary>
    public int CompareTo(ResourceKey other)
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

    public static bool operator ==(ResourceKey left, ResourceKey right) => left.Equals(right);
    public static bool operator !=(ResourceKey left, ResourceKey right) => !left.Equals(right);
    /// <summary>
    /// Returns string representation of this key.
    /// </summary>
    public override string ToString()
    {
        return $"{name}";
    }

    /// <summary>
    /// Returns key value used by prefab-mappable interfaces.
    /// </summary>
    public FixedString64Bytes GetKey()
    {
        return name;
    }
}

/// <summary>
/// Supported resource categories.
/// </summary>
public enum ResourceType
{
    None,
    Food,
    Ore,
    Construction,
    Misc    
}
