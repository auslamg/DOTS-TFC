using System;
using System.Collections.Generic;
using Dto;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dto.Buildings
{
    [Serializable]
    public struct DtoBuildingData
    {
        public float3 position;
        public quaternion rotation;
        public string prefabKey;
        public int ownerID;
        public uint factionID;
        public bool selected;
        public DtoTrainerData trainerData;
        public int currentHealth;

        public override string ToString()
        {
            return $"SaveBuildingData(" +
                   $"prefabKey: {prefabKey}, " +
                   $"ownerID: {ownerID}, " +
                   $"factionID: {factionID}, " +
                   $"selected: {selected}, " +
                   $"position: ({position.x}, {position.y}, {position.z}), " +
                   $"rotation: ({rotation.value.x}, {rotation.value.y}, {rotation.value.z}, {rotation.value.w}), " +
                   $"currentHealth: {currentHealth})";
        }

        public string ToJson()
        {
            var serializable = new SerializableBuildingData
            {
                position = new Float3Serializable(position),
                rotation = new QuaternionSerializable(rotation),
                prefabKey = prefabKey,
                ownerID = ownerID,
                factionID = factionID,
                selected = selected,
                currentHealth = currentHealth,
                trainerData = trainerData
            };

            return JsonUtility.ToJson(serializable, true);
        }

        public static DtoBuildingData FromJson(string json)
        {
            var data = JsonUtility.FromJson<SerializableBuildingData>(json);

            return new DtoBuildingData
            {
                position = new float3(data.position.x, data.position.y, data.position.z),
                rotation = new quaternion(data.rotation.x, data.rotation.y, data.rotation.z, data.rotation.w),

                prefabKey = data.prefabKey,
                ownerID = data.ownerID,
                factionID = data.factionID,

                selected = data.selected,
                trainerData = data.trainerData,

                currentHealth = data.currentHealth
            };
        }

        [Serializable]
        private struct SerializableBuildingData
        {
            public Float3Serializable position;
            public QuaternionSerializable rotation;

            public string prefabKey;
            public int ownerID;
            public uint factionID;

            public bool selected;

            public DtoTrainerData trainerData;

            public int currentHealth;
        }
    }

    [Serializable]
    public struct DtoTrainerData
    {
        public float currentProgress;
        public float maxProgress;

        public string activeUnitKey;

        public Float3Serializable spawnPointOffset;
        public Float3Serializable rallyPositionOffset;

        public bool onUnitQueueChange;

        public List<string> trainingQueue;

        public static DtoTrainerData FromTrainer(
            Trainer trainer,
            DynamicBuffer<QueuedUnitBuffer> queueBuffer)
        {
            var dto = new DtoTrainerData
            {
                currentProgress = trainer.currentProgress,
                maxProgress = trainer.maxProgress,
                activeUnitKey = trainer.activeUnitKey.name.ToString(),
                spawnPointOffset = new Float3Serializable(trainer.spawnPointOffset),
                rallyPositionOffset = new Float3Serializable(trainer.rallyPositionOffset),
                onUnitQueueChange = trainer.onUnitQueueChange,
                trainingQueue = new List<string>()
            };

            foreach (var q in queueBuffer)
            {
                dto.trainingQueue.Add(q.unitKey.name.ToString());
            }

            return dto;
        }

        public Trainer ToTrainer()
        {
            return new Trainer
            {
                currentProgress = currentProgress,
                maxProgress = maxProgress,
                activeUnitKey = new UnitKey
                {
                    name = new FixedString64Bytes(activeUnitKey)
                },
                spawnPointOffset = spawnPointOffset.ToFloat3(),
                rallyPositionOffset = rallyPositionOffset.ToFloat3(),
                onUnitQueueChange = onUnitQueueChange
            };
        }

        public void RewriteQueuedUnitBuffer(DynamicBuffer<QueuedUnitBuffer> buffer)
        {
            buffer.Clear();

            if (trainingQueue == null)
                return;

            foreach (var key in trainingQueue)
            {
                buffer.Add(new QueuedUnitBuffer
                {
                    unitKey = new UnitKey
                    {
                        name = new FixedString64Bytes(key)
                    }
                });
            }
        }

        public string ToJson()
        {
            var serializable = new SerializableTrainerData
            {
                currentProgress = currentProgress,
                maxProgress = maxProgress,
                activeUnitKey = activeUnitKey,
                spawnPointOffset = spawnPointOffset,
                rallyPositionOffset = rallyPositionOffset,
                onUnitQueueChange = onUnitQueueChange,
                trainingQueue = trainingQueue
            };

            return JsonUtility.ToJson(serializable, true);
        }

        public static DtoTrainerData FromJson(string json)
        {
            var data = JsonUtility.FromJson<SerializableTrainerData>(json);

            return new DtoTrainerData
            {
                currentProgress = data.currentProgress,
                maxProgress = data.maxProgress,
                activeUnitKey = data.activeUnitKey,
                spawnPointOffset = data.spawnPointOffset,
                rallyPositionOffset = data.rallyPositionOffset,
                onUnitQueueChange = data.onUnitQueueChange,
                trainingQueue = data.trainingQueue
            };
        }

        [Serializable]
        private struct SerializableTrainerData
        {
            public float currentProgress;
            public float maxProgress;
            public string activeUnitKey;
            public Float3Serializable spawnPointOffset;
            public Float3Serializable rallyPositionOffset;
            public bool onUnitQueueChange;
            public List<string> trainingQueue;
        }
    }
}
