using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScriptableObject describing gameplay and UI data for one unit type.
/// </summary>
[CreateAssetMenu(fileName = "UnitDataSO", menuName = "Units/UnitDataSO")]
public class UnitDataSO : ScriptableObject
{
    /// <summary>
    /// Category of this unit.
    /// </summary>
    [SerializeField]
    [Tooltip("Category/type of this unit.")]
    public UnitType unitType;

    /// <summary>
    /// Time required to train this unit.
    /// </summary>
    [SerializeField]
    [Tooltip("Training time required before this unit is produced.")]
    public float trainingTime;

    /// <summary>
    /// Card sprite used by UI lists and buttons.
    /// </summary>
    [SerializeField]
    [Tooltip("Sprite shown for this unit in UI cards/buttons.")]
    public Sprite imageCard;

    [SerializeField, HideInInspector]
    private UnitKey cachedKey;

    /// <summary>
    /// Deterministic key generated from the asset name.
    /// </summary>
    public UnitKey unitKey => cachedKey;

    /// <summary>
    /// Refreshes cached key data whenever the asset is modified in the editor.
    /// </summary>
    private void OnValidate()
    {
        cachedKey = new UnitKey
        {
            name = this.name
        };
    }
}

/// <summary>
/// Unique identifier for a <see cref="UnitData"/> struct, obtained from the SO name.
/// </summary>
public struct UnitKey : IEquatable<UnitKey>, IComparable<UnitKey>, IEntityPrefabMappable
{
    /// <summary>
    /// Fixed-string key value.
    /// </summary>
    public FixedString64Bytes name;

    /// <summary>
    /// Compares two keys for equality.
    /// </summary>
    public bool Equals(UnitKey other)
    {
        return name.Equals(other.name);
    }

    /// <summary>
    /// Compares this key to another object for equality.
    /// </summary>
    public override bool Equals(object obj)
    {
        return obj is UnitKey other && Equals(other);
    }

    /// <summary>
    /// Compares this key with another key for sorting.
    /// </summary>
    public int CompareTo(UnitKey other)
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

    public static bool operator ==(UnitKey left, UnitKey right) => left.Equals(right);
    public static bool operator !=(UnitKey left, UnitKey right) => !left.Equals(right);
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
/// Supported unit categories.
/// </summary>
public enum UnitType
{
    None,
    Peaceful,
    Melee,
    Ranged,
}
