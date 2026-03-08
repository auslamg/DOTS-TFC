using System;
using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingDataSO", menuName = "Buildings/BuildingDataSO")]
public class BuildingDataSO : ScriptableObject
{
    public BuildingType buildingType;

    [SerializeField, HideInInspector]
    private BuildingKey cachedKey;
    public BuildingKey buildingKey => cachedKey;

    private void OnValidate()
    {
        cachedKey = new BuildingKey
        {
            name = this.name
        };
    }
}

public struct BuildingKey : IEquatable<BuildingKey>, IComparable<BuildingKey>
{
    public FixedString64Bytes name;
    public bool Equals(BuildingKey other)
    {
        return name.Equals(other.name);
    }
    public override bool Equals(object obj)
    {
        return obj is BuildingKey other && Equals(other);
    }
    public int CompareTo(BuildingKey other)
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

    public static bool operator ==(BuildingKey left, BuildingKey right) => left.Equals(right);
    public static bool operator !=(BuildingKey left, BuildingKey right) => !left.Equals(right);
    public override string ToString()
    {
        return $"{name}";
    }
}

public enum BuildingType
{
    None,
    Tower,
    Trainer,
    Spawner,
    Production
}
