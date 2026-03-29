using System;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// ScriptableObject describing gameplay and presentation data for one building type.
/// </summary>
[CreateAssetMenu(fileName = "BuildingDataSO", menuName = "Buildings/BuildingDataSO")]
public class BuildingDataSO : ScriptableObject
{
    /// <summary>
    /// Category of this building.
    /// </summary>
    [SerializeField]
    [Tooltip("Category/type of this building.")]
    public BuildingType buildingType;

    /// <summary>
    /// Source prefab converted and spawned for this building.
    /// </summary>
    [SerializeField]
    [Tooltip("Prefab GameObject used when placing or spawning this building.")]
    public GameObject prefabGO;

    /// <summary>
    /// Minimum allowed distance to other buildings of the same type.
    /// </summary>
    [SerializeField]
    [Tooltip("Minimum spacing required between buildings of the same type.")]
    public float minDistanceToSimilar;

    /// <summary>
    /// Whether this building appears as a player-buildable option.
    /// </summary>
    [SerializeField]
    [Tooltip("If enabled, this building appears in buildable UI lists.")]
    public bool isBuildable;

    /// <summary>
    /// Card sprite used by UI lists and buttons.
    /// </summary>
    [SerializeField]
    [Tooltip("Sprite shown for this building in UI cards/buttons.")]
    public Sprite imageCard;

    /// <summary>
    /// Ghost prefab used for placement preview visualization.
    /// </summary>
    [SerializeField]
    [Tooltip("Ghost prefab used for building placement preview.")]
    public GameObject buildingGhostPrefab;

    [SerializeField, HideInInspector]
    private BuildingKey cachedKey;

    /// <summary>
    /// Deterministic key generated from the asset name.
    /// </summary>
    public BuildingKey buildingKey => cachedKey;

    /// <summary>
    /// Refreshes cached key data whenever the asset is modified in the editor.
    /// </summary>
    private void OnValidate()
    {
        cachedKey = new BuildingKey
        {
            name = this.name
        };
    }

    /// <summary>
    /// Returns whether this entry represents the special None building.
    /// </summary>
    /// <returns><see langword="true"/> when <see cref="buildingType"/> is <see cref="BuildingType.None"/>.</returns>
    public bool IsNone()
    {
        return this.buildingType == BuildingType.None;
    }
}

/// <summary>
/// Unique identifier for a <see cref="BuildingData"/> struct, obtained from the SO name.
/// </summary>
public struct BuildingKey : IEquatable<BuildingKey>, IComparable<BuildingKey>, IEntityPrefabMappable
{
    /// <summary>
    /// Fixed-string key value.
    /// </summary>
    public FixedString64Bytes name;

    /// <summary>
    /// Compares two keys for equality.
    /// </summary>
    public bool Equals(BuildingKey other)
    {
        return name.Equals(other.name);
    }

    /// <summary>
    /// Compares this key to another object for equality.
    /// </summary>
    public override bool Equals(object obj)
    {
        return obj is BuildingKey other && Equals(other);
    }

    /// <summary>
    /// Compares this key with another key for sorting.
    /// </summary>
    public int CompareTo(BuildingKey other)
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

    public static bool operator ==(BuildingKey left, BuildingKey right) => left.Equals(right);
    public static bool operator !=(BuildingKey left, BuildingKey right) => !left.Equals(right);
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
/// Supported building categories.
/// </summary>
public enum BuildingType
{
    None,
    Tower,
    Trainer,
    Spawner,
    Production,
    Fort
}
