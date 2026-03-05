using System;
using Unity.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingSO", menuName = "Buildings/BuildingSO")]
public class BuildingSO : ScriptableObject
{
    [SerializeField] string buildingName;
    [SerializeField] BuildingType buildingType;

    [SerializeField, HideInInspector]
    private BuildingKey cachedKey;

    public BuildingKey buildingKey => cachedKey;

    private void OnValidate()
    {
        cachedKey = new BuildingKey
        {
            name = buildingName,
            buildingType = buildingType
        };
    }
}

public struct BuildingKey : IEquatable<BuildingKey>
{   
    public FixedString64Bytes name;
    public BuildingType buildingType;
    public override bool Equals(object obj)
    {
        if (!(obj is BuildingKey))
            return false;

        BuildingKey other = (BuildingKey)obj;
        return this.name == other.name;
    }

    public bool Equals(BuildingKey other)
    {
        return name.Equals(other.name);
    }

    public override string ToString()
    {
        return name + "[BuildingType:" + buildingType + "]";
    }

    public static bool operator ==(BuildingKey key1, BuildingKey key2)
    {
        return key1.Equals(key2);
    }

    public static bool operator !=(BuildingKey key1, BuildingKey key2)
    {
        return !key1.Equals(key2);
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
