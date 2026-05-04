using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct SaveGameData
{
    public List<SaveUnitData> units;
    public List<SaveBuildingData> buildings;
    public SaveResourceData resources;
    public SaveManagedData managed;
}

[Serializable]
public struct SaveUnitData
{
    public float3 position;
    public quaternion rotation;
    public string prefabKey;
    public int ownerID;
    public float3 movePosition;
    public Entity targetEntity;
    public int currentHealth;
    public uint factionID;

    public override string ToString()
    {
        return $"UnitSaveData(" +
               $"prefabKey: {prefabKey}, " +
               $"ownerID: {ownerID}, " +
               $"position: ({position.x}, {position.y}, {position.z}), " +
               $"rotation: ({rotation.value.x}, {rotation.value.y}, {rotation.value.z}, {rotation.value.w}), " +
               $"movePosition: ({movePosition.x}, {movePosition.y}, {movePosition.z}), " +
               $"targetEntity: (Index: {targetEntity.Index}, Version: {targetEntity.Version}), " +
               $"currentHealth: {currentHealth}, " +
               $"factionID: {factionID})";
    }

    public string ToJson()
    {
        var serializable = new SerializableUnitData
        {
            position = new Float3Serializable(position),
            rotation = new QuaternionSerializable(rotation),
            prefabKey = prefabKey,
            unitOwner = ownerID,
            movePosition = new Float3Serializable(movePosition),
            targetEntity = new EntitySerializable(targetEntity),
            currentHealth = currentHealth,
            factionID = factionID
        };
        return JsonUtility.ToJson(serializable, true);
    }

    public static SaveUnitData FromJson(string json)
    {
        var data = JsonUtility.FromJson<SerializableUnitData>(json);

        return new SaveUnitData
        {
            position = new float3(
                data.position.x,
                data.position.y,
                data.position.z
            ),
            rotation = new quaternion(
                data.rotation.x,
                data.rotation.y,
                data.rotation.z,
                data.rotation.w
            ),
            prefabKey = data.prefabKey,
            ownerID = data.unitOwner,
            movePosition = new float3(
                data.movePosition.x,
                data.movePosition.y,
                data.movePosition.z
            ),
            targetEntity = new Entity
            {
                Index = data.targetEntity.index,
                Version = data.targetEntity.version
            },
            currentHealth = data.currentHealth,
            factionID = data.factionID
        };
    }

    [Serializable]
    private struct SerializableUnitData
    {
        public Float3Serializable position;
        public QuaternionSerializable rotation;
        public string prefabKey;
        public int unitOwner;
        public Float3Serializable movePosition;
        public EntitySerializable targetEntity;
        public int currentHealth;
        public uint factionID;
    }
    
}

[Serializable]
public struct SaveBuildingData
{
    public float3 position;
    public quaternion rotation;
    public string prefabKey;
    public int ownerID;
    public int currentHealth;
    public uint factionID;

    public override string ToString()
    {
        return $"SaveBuildingData(" +
               $"prefabKey: {prefabKey}, " +
               $"ownerID: {ownerID}, " +
               $"position: ({position.x}, {position.y}, {position.z}), " +
               $"rotation: ({rotation.value.x}, {rotation.value.y}, {rotation.value.z}, {rotation.value.w}), " +
               $"currentHealth: {currentHealth}, " +
               $"factionID: {factionID})";
    }

    public string ToJson()
    {
        var serializable = new SerializableBuildingData
        {
            position = new Float3Serializable(position),
            rotation = new QuaternionSerializable(rotation),
            prefabKey = prefabKey,
            ownerID = ownerID,
            currentHealth = currentHealth,
            factionID = factionID
        };

        return JsonUtility.ToJson(serializable, true);
    }

    public static SaveBuildingData FromJson(string json)
    {
        var data = JsonUtility.FromJson<SerializableBuildingData>(json);

        return new SaveBuildingData
        {
            position = new float3(
                data.position.x,
                data.position.y,
                data.position.z
            ),
            rotation = new quaternion(
                data.rotation.x,
                data.rotation.y,
                data.rotation.z,
                data.rotation.w
            ),
            prefabKey = data.prefabKey,
            ownerID = data.ownerID,
            currentHealth = data.currentHealth,
            factionID = data.factionID
        };
    }

    [Serializable]
    private struct SerializableBuildingData
    {
        public Float3Serializable position;
        public QuaternionSerializable rotation;
        public string prefabKey;
        public int ownerID;
        public int currentHealth;
        public uint factionID;
    }
}

[Serializable]
public struct SaveResourceData
{
    public List<SaveResourceEntry> resources;

    public override string ToString()
    {
        string result = "SaveResourceData:\n";
        if (resources != null)
        {
            foreach (var r in resources)
            {
                result += $"- {r.resourceKey.name}: {r.amount}\n";
            }
        }
        return result;
    }

    // Create from your runtime dictionary
    public static SaveResourceData FromDictionary(Dictionary<ResourceKey, int> dict)
    {
        var data = new SaveResourceData
        {
            resources = new List<SaveResourceEntry>()
        };

        foreach (var kv in dict)
        {
            data.resources.Add(new SaveResourceEntry
            {
                resourceKey = kv.Key,
                amount = kv.Value
            });
        }

        return data;
    }

    // Convert back to runtime dictionary
    public Dictionary<ResourceKey, int> ToDictionary()
    {
        var dict = new Dictionary<ResourceKey, int>();

        if (resources == null)
            return dict;

        foreach (var entry in resources)
        {
            dict[entry.resourceKey] = entry.amount;
        }

        return dict;
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    public static SaveResourceData FromJson(string json)
    {
        return JsonUtility.FromJson<SaveResourceData>(json);
    }

    [Serializable]
    public struct SaveResourceEntry
    {
        public ResourceKey resourceKey;
        public int amount;
    }
}

[Serializable]
public struct SaveManagedData
{
    public float3 camPosition;
    public quaternion camRotation;

    public override string ToString()
    {
        return $"SaveManagedData(" +
               $"camPosition: ({camPosition.x}, {camPosition.y}, {camPosition.z}), " +
               $"camRotation: ({camRotation.value.x}, {camRotation.value.y}, {camRotation.value.z}, {camRotation.value.w}))";
    }

    public string ToJson()
    {
        var serializable = new SerializableManagedData
        {
            position = new Float3Serializable(camPosition),
            rotation = new QuaternionSerializable(camRotation)
        };

        return JsonUtility.ToJson(serializable, true);
    }

    public static SaveManagedData FromJson(string json)
    {
        var data = JsonUtility.FromJson<SerializableManagedData>(json);

        return new SaveManagedData
        {
            camPosition = data.position.ToFloat3(),
            camRotation = data.rotation.ToQuaternion()
        };
    }

    [Serializable]
    private struct SerializableManagedData
    {
        public Float3Serializable position;
        public QuaternionSerializable rotation;
    }
}


[Serializable]
public struct Float3Serializable
{
    public float x, y, z;

    public Float3Serializable(float3 v)
    {
        x = v.x;
        y = v.y;
        z = v.z;
    }

    public float3 ToFloat3()
    {
        return new float3(x, y, z);
    }
}

[Serializable]
public struct QuaternionSerializable
{
    public float x, y, z, w;

    public QuaternionSerializable(quaternion q)
    {
        x = q.value.x;
        y = q.value.y;
        z = q.value.z;
        w = q.value.w;
    }

    public quaternion ToQuaternion()
    {
        return new quaternion(x, y, z, w);
    }
}

[Serializable]
public struct EntitySerializable
{
    public int index;
    public int version;

    public EntitySerializable(Entity e)
    {
        index = e.Index;
        version = e.Version;
    }

    public Entity ToEntity()
    {
        return new Entity
        {
            Index = index,
            Version = version
        };
    }
}