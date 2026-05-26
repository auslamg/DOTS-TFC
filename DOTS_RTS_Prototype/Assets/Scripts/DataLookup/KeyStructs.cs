using System;
using Dto;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Unique identifier for a <see cref="UnitData"/> struct, obtained from the SO name.
/// </summary>
[Serializable]
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

    /// <summary>
    /// Converts the given struct to Json format.
    /// </summary>
    public string ToJson()
    {
        KeySerializable serializable = new KeySerializable
        {
            name = name.ToString()
        };

        return JsonUtility.ToJson(serializable, true);
    }

    /// <summary>
    /// Converts the received Json file to a <see cref="UnitKey"/> struct.
    /// </summary>
    public static UnitKey FromJson(string json)
    {
        KeySerializable data = JsonUtility.FromJson<KeySerializable>(json);

        return new UnitKey
        {
            name = SerializationUtil.ParseFixedString64Bytes(data.name)
        };
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

/// <summary>
/// Unique identifier for a <see cref="BuildingData"/> struct, obtained from the SO name.
/// </summary>
[Serializable]
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

    /// <summary>
    /// Converts the given struct to Json format.
    /// </summary>
    public string ToJson()
    {
        var serializable = new KeySerializable
        {
            name = name.ToString()
        };

        return JsonUtility.ToJson(serializable, true);
    }

    /// <summary>
    /// Converts the received Json file to a <see cref="BuildingKey"/> struct.
    /// </summary>
    public static BuildingKey FromJson(string json)
    {
        var data = JsonUtility.FromJson<KeySerializable>(json);

        return new BuildingKey
        {
            name = SerializationUtil.ParseFixedString64Bytes(data.name)
        };
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
    Harvester,
    Fort,
    Producer,
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

    /// <summary>
    /// Converts the given struct to Json format.
    /// </summary>
    public string ToJson()
    {
        KeySerializable serializable = new KeySerializable
        {
            name = name.ToString()
        };

        return JsonUtility.ToJson(serializable, true);
    }

    /// <summary>
    /// Converts the received Json file to a <see cref="ResourceKey"/> struct.
    /// </summary>
    public static ResourceKey FromJson(string json)
    {
        KeySerializable data = JsonUtility.FromJson<KeySerializable>(json);

        return new ResourceKey
        {
            name = SerializationUtil.ParseFixedString64Bytes(data.name)
        };
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

/// <summary>
/// Unique identifier for a <see cref="EntityPrefab"/> struct, obtained from the prefab name.
/// </summary>
[Serializable]
public struct EntityPrefabKey : IEquatable<EntityPrefabKey>, IComparable<EntityPrefabKey>, IComparable<IEntityPrefabMappable>
{
    /// <summary>
    /// Name-based key used to identify a prefab entry.
    /// </summary>
    public FixedString64Bytes name;
    public bool Equals(EntityPrefabKey other)
    {
        return name.Equals(other.name);
    }
    public override bool Equals(object obj)
    {
        return obj is EntityPrefabKey other && Equals(other);
    }
    public int CompareTo(EntityPrefabKey other)
    {
        int cmp = name.CompareTo(other.name);
        return cmp;
    }

    public int CompareTo(IEntityPrefabMappable other)
    {
        int cmp = name.CompareTo(other.GetKey());
        return cmp;
    }

    public static EntityPrefabKey From(IEntityPrefabMappable other)
    {
        return new EntityPrefabKey { name = other.GetKey(), };
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

    public static bool operator ==(EntityPrefabKey left, EntityPrefabKey right) => left.Equals(right);
    public static bool operator !=(EntityPrefabKey left, EntityPrefabKey right) => !left.Equals(right);
    public override string ToString()
    {
        return $"{name}";
    }

    /// <summary>
    /// Converts the given struct to Json format.
    /// </summary>
    public string ToJson()
    {
        KeySerializable serializable = new KeySerializable
        {
            name = name.ToString()
        };

        return JsonUtility.ToJson(serializable, true);
    }

    /// <summary>
    /// Converts the received Json file to a <see cref="EntityPrefabKey"/> struct.
    /// </summary>
    public static EntityPrefabKey FromJson(string json)
    {
        KeySerializable data = JsonUtility.FromJson<KeySerializable>(json);

        return new EntityPrefabKey
        {
            name = SerializationUtil.ParseFixedString64Bytes(data.name)
        };
    }
}

/// <summary>
/// Interface for types that can expose an <see cref="EntityPrefabKey"/> comparable key.
/// </summary>
public interface IEntityPrefabMappable
{
    /// <summary>
    /// Retrieves the key used for prefab registry comparisons and lookups.
    /// </summary>
    FixedString64Bytes GetKey();
}

[Serializable]
public struct ResourceQuantity
{
    public ResourceSO resourceSO;
    public int amount;
}