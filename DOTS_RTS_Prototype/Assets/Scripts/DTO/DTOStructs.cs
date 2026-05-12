using System;
using System.Collections.Generic;
using Dto.Buildings;
using Dto.Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dto
{
    [Serializable]
    public struct DtoGameData
    {
        public List<DtoUnitData> units;
        public List<DtoBuildingData> buildings;
        public DtoResourceData resources;
        public DtoManagedData managed;
    }

    [Serializable]
    public struct DtoResourceData
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

        public static DtoResourceData FromDictionary(Dictionary<ResourceKey, int> dict)
        {
            var data = new DtoResourceData
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

        public static DtoResourceData FromJson(string json)
        {
            return JsonUtility.FromJson<DtoResourceData>(json);
        }

        [Serializable]
        public struct SaveResourceEntry
        {
            public ResourceKey resourceKey;
            public int amount;
        }
    }

    [Serializable]
    public struct DtoManagedData
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

        public static DtoManagedData FromJson(string json)
        {
            var data = JsonUtility.FromJson<SerializableManagedData>(json);

            return new DtoManagedData
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

    /// <summary>
    /// Key wrapper for data serialization, used for <see cref="UnitKey"/>,
    /// <see cref="BuildingKey"/>, <see cref="ResourceKey"/> and <see cref="EntityPrefabKey"/>.
    /// </summary>
    [Serializable]
    public struct KeySerializable
    {
        public string name;
    }
}
