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
    /// <summary>
    /// Represents the complete serialized game state.
    /// </summary>
    [Serializable]
    public struct DtoGameData
    {
        /// <summary>
        /// Serialized unit data.
        /// </summary>
        public List<DtoUnitData> units;

        /// <summary>
        /// Serialized building data.
        /// </summary>
        public List<DtoBuildingData> buildings;

        /// <summary>
        /// Serialized resource data.
        /// </summary>
        public DtoResourceData resources;

        /// <summary>
        /// Serialized managed scene data.
        /// </summary>
        public DtoManagedData managed;
    }

    /// <summary>
    /// Represents serialized resource ownership data.
    /// </summary>
    [Serializable]
    public struct DtoResourceData
    {
        /// <summary>
        /// Serialized resource entries.
        /// </summary>
        public List<SaveResourceEntry> resources;

        /// <summary>
        /// Returns a formatted string representation of the resource data.
        /// </summary>
        /// <returns>A formatted resource data string.</returns>
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

        /// <summary>
        /// Creates a DTO resource container from a resource dictionary.
        /// </summary>
        /// <param name="dict">The source resource dictionary.</param>
        /// <returns>A populated <see cref="DtoResourceData"/> instance.</returns>
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

        /// <summary>
        /// Converts the serialized resource data into a dictionary.
        /// </summary>
        /// <returns>A dictionary containing all resource entries.</returns>
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

        /// <summary>
        /// Serializes the resource data into JSON format.
        /// </summary>
        /// <returns>A formatted JSON string.</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// Deserializes resource data from JSON.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A populated <see cref="DtoResourceData"/> instance.</returns>
        public static DtoResourceData FromJson(string json)
        {
            return JsonUtility.FromJson<DtoResourceData>(json);
        }

        /// <summary>
        /// Represents a serialized resource entry.
        /// </summary>
        [Serializable]
        public struct SaveResourceEntry
        {
            /// <summary>
            /// The resource identifier.
            /// </summary>
            public ResourceKey resourceKey;

            /// <summary>
            /// The stored amount of the resource.
            /// </summary>
            public int amount;
        }
    }

    /// <summary>
    /// Represents serialized managed scene data.
    /// </summary>
    [Serializable]
    public struct DtoManagedData
    {
        /// <summary>
        /// Camera-related managed data.
        /// </summary>
        public DtoCameraData camera;

        /// <summary>
        /// Horde / wave progression managed data.
        /// </summary>
        public DtoHordeData horde;

        /// <summary>
        /// Returns a formatted string representation of the managed data.
        /// </summary>
        /// <returns>A formatted managed data string.</returns>
        public override string ToString()
        {
            return $"SaveManagedData(camera: {camera.ToString()}, horde: {horde.ToString()})";
        }

        /// <summary>
        /// Serializes the managed data into JSON format.
        /// </summary>
        /// <returns>A formatted JSON string.</returns>
        public string ToJson()
        {
            var serializable = new SerializableManagedData
            {
                camera = new SerializableCamera
                {
                    position = new Float3Serializable(camera.camPosition),
                    rotation = new QuaternionSerializable(camera.camRotation)
                },
                horde = new SerializableHorde
                {
                    currentWaveIndex = horde.currentWaveIndex,
                    currentState = horde.currentState,
                    currentStateTimer = horde.currentStateTimer,
                    currentTimerInterval = horde.currentTimerInterval,
                    isCountingDownToNextWave = horde.isCountingDownToNextWave,
                    remainingNextWaveTime = horde.remainingNextWaveTime,
                    nextWaveInterval = horde.nextWaveInterval,
                    currentSpawnEntryIndex = horde.currentSpawnEntryIndex,
                    currentEntryIndex = horde.currentEntryIndex,
                    currentSpawnedInEntry = horde.currentSpawnedInEntry,
                    spawnEntryRemainingInterval = horde.spawnEntryRemainingInterval,
                    spawnEntryPostCooldownRemaining = horde.spawnEntryPostCooldownRemaining,
                    finalWave = horde.finalWave,
                    lastPoolIndex = horde.lastPoolIndex
                }
            };

            return JsonUtility.ToJson(serializable, true);
        }

        /// <summary>
        /// Deserializes managed data from JSON.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A populated <see cref="DtoManagedData"/> instance.</returns>
        public static DtoManagedData FromJson(string json)
        {
            var data = JsonUtility.FromJson<SerializableManagedData>(json);

            return new DtoManagedData
            {
                camera = new DtoCameraData
                {
                    camPosition = data.camera.position.ToFloat3(),
                    camRotation = data.camera.rotation.ToQuaternion()
                },
                horde = new DtoHordeData
                {
                    currentWaveIndex = data.horde.currentWaveIndex,
                    currentState = data.horde.currentState,
                    currentStateTimer = data.horde.currentStateTimer,
                    currentTimerInterval = data.horde.currentTimerInterval,
                    isCountingDownToNextWave = data.horde.isCountingDownToNextWave,
                    remainingNextWaveTime = data.horde.remainingNextWaveTime,
                    nextWaveInterval = data.horde.nextWaveInterval,
                    currentSpawnEntryIndex = data.horde.currentSpawnEntryIndex,
                    currentEntryIndex = data.horde.currentEntryIndex,
                    currentSpawnedInEntry = data.horde.currentSpawnedInEntry,
                    spawnEntryRemainingInterval = data.horde.spawnEntryRemainingInterval,
                    spawnEntryPostCooldownRemaining = data.horde.spawnEntryPostCooldownRemaining,
                    finalWave = data.horde.finalWave,
                    lastPoolIndex = data.horde.lastPoolIndex
                }
            };
        }

        /// <summary>
        /// Internal serializable representation of managed data.
        /// </summary>
        [Serializable]
        private struct SerializableManagedData
        {
            /// <summary>
            /// Serialized camera position.
            /// </summary>
            public SerializableCamera camera;
            public SerializableHorde horde;
        }

        [Serializable]
        private struct SerializableCamera
        {
            public Float3Serializable position;
            public QuaternionSerializable rotation;
        }

        [Serializable]
        private struct SerializableHorde
        {
            public int currentWaveIndex;
            public int currentState;
            public float currentStateTimer;
            public float currentTimerInterval;
            public bool isCountingDownToNextWave;
            public float remainingNextWaveTime;
            public float nextWaveInterval;
            public int currentSpawnEntryIndex;
            public int currentEntryIndex;
            public int currentSpawnedInEntry;
            public float spawnEntryRemainingInterval;
            public float spawnEntryPostCooldownRemaining;
            public bool finalWave;
            public int lastPoolIndex;
        }
    }

    [Serializable]
    public struct DtoCameraData
    {
        public float3 camPosition;
        public quaternion camRotation;

        public override string ToString()
        {
            return $"Camera(pos: ({camPosition.x},{camPosition.y},{camPosition.z}), rot: ({camRotation.value.x},{camRotation.value.y},{camRotation.value.z},{camRotation.value.w}))";
        }
    }

    [Serializable]
    public struct DtoHordeData
    {
        public int currentWaveIndex;
        public int currentState;
        public float currentStateTimer;
        public float currentTimerInterval;
        public bool isCountingDownToNextWave;
        public float remainingNextWaveTime;
        public float nextWaveInterval;
        public int currentSpawnEntryIndex;
        public int currentEntryIndex;
        public int currentSpawnedInEntry;
        public float spawnEntryRemainingInterval;
        public float spawnEntryPostCooldownRemaining;
        public bool finalWave;
        public int lastPoolIndex;

        public override string ToString()
        {
            return $"Horde(wave:{currentWaveIndex}, state:{currentState}, counting:{isCountingDownToNextWave}, remainingNext:{remainingNextWaveTime})";
        }
    }

    /// <summary>
    /// Serializable wrapper for <see cref="float3"/>.
    /// </summary>
    [Serializable]
    public struct Float3Serializable
    {
        /// <summary>
        /// The X component.
        /// </summary>
        public float x;

        /// <summary>
        /// The Y component.
        /// </summary>
        public float y;

        /// <summary>
        /// The Z component.
        /// </summary>
        public float z;

        /// <summary>
        /// Initializes the wrapper from a <see cref="float3"/> value.
        /// </summary>
        /// <param name="v">The source vector.</param>
        public Float3Serializable(float3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        /// <summary>
        /// Converts the wrapper into a <see cref="float3"/>.
        /// </summary>
        /// <returns>A reconstructed <see cref="float3"/>.</returns>
        public float3 ToFloat3()
        {
            return new float3(x, y, z);
        }
    }

    /// <summary>
    /// Serializable wrapper for <see cref="quaternion"/>.
    /// </summary>
    [Serializable]
    public struct QuaternionSerializable
    {
        /// <summary>
        /// The X component.
        /// </summary>
        public float x;

        /// <summary>
        /// The Y component.
        /// </summary>
        public float y;

        /// <summary>
        /// The Z component.
        /// </summary>
        public float z;

        /// <summary>
        /// The W component.
        /// </summary>
        public float w;

        /// <summary>
        /// Initializes the wrapper from a <see cref="quaternion"/> value.
        /// </summary>
        /// <param name="q">The source quaternion.</param>
        public QuaternionSerializable(quaternion q)
        {
            x = q.value.x;
            y = q.value.y;
            z = q.value.z;
            w = q.value.w;
        }

        /// <summary>
        /// Converts the wrapper into a <see cref="quaternion"/>.
        /// </summary>
        /// <returns>A reconstructed <see cref="quaternion"/>.</returns>
        public quaternion ToQuaternion()
        {
            return new quaternion(x, y, z, w);
        }
    }

    /// <summary>
    /// Serializable wrapper for <see cref="Entity"/>.
    /// </summary>
    [Serializable]
    public struct EntitySerializable
    {
        /// <summary>
        /// The entity index.
        /// </summary>
        public int index;

        /// <summary>
        /// The entity version.
        /// </summary>
        public int version;

        /// <summary>
        /// Initializes the wrapper from an entity instance.
        /// </summary>
        /// <param name="e">The source entity.</param>
        public EntitySerializable(Entity e)
        {
            index = e.Index;
            version = e.Version;
        }

        /// <summary>
        /// Converts the wrapper into an <see cref="Entity"/>.
        /// </summary>
        /// <returns>A reconstructed entity.</returns>
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
        /// <summary>
        /// The serialized key name.
        /// </summary>
        public string name;
    }
}
