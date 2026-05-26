using System;
using System.Collections.Generic;
using Dto;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static SerializationUtil;

namespace Dto.Buildings
{
    /// <summary>
    /// Data transfer object containing serialized building state data.
    /// </summary>
    [Serializable]
    public struct DtoBuildingData
    {
        /// <summary>
        /// The world position of the building.
        /// </summary>
        public float3 position;

        /// <summary>
        /// The world rotation of the building.
        /// </summary>
        public quaternion rotation;

        /// <summary>
        /// The prefab identifier used to recreate the building.
        /// </summary>
        public string prefabKey;

        /// <summary>
        /// The owning player identifier.
        /// </summary>
        public int ownerID;

        /// <summary>
        /// The faction identifier assigned to the building.
        /// </summary>
        public uint factionID;

        /// <summary>
        /// Indicates whether the building is currently selected.
        /// </summary>
        public bool selected;

        /// <summary>
        /// Serialized trainer component data associated with the building.
        /// </summary>
        public DtoTrainerData trainerData;

        /// <summary>
        /// The current health value of the building.
        /// </summary>
        public int currentHealth;

        /// <summary>
        /// Returns a formatted string representation of the building data.
        /// </summary>
        /// <returns>A formatted string containing building state information.</returns>
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

        /// <summary>
        /// Serializes the building data to JSON format.
        /// </summary>
        /// <returns>A formatted JSON string representing the building data.</returns>
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

        /// <summary>
        /// Deserializes building data from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string containing serialized building data.</param>
        /// <returns>A populated <see cref="DtoBuildingData"/> instance.</returns>
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

        /// <summary>
        /// Internal serializable representation used for JSON conversion.
        /// </summary>
        [Serializable]
        private struct SerializableBuildingData
        {
            /// <summary>
            /// Serialized position value.
            /// </summary>
            public Float3Serializable position;

            /// <summary>
            /// Serialized rotation value.
            /// </summary>
            public QuaternionSerializable rotation;

            /// <summary>
            /// Serialized prefab identifier.
            /// </summary>
            public string prefabKey;

            /// <summary>
            /// Serialized owner identifier.
            /// </summary>
            public int ownerID;

            /// <summary>
            /// Serialized faction identifier.
            /// </summary>
            public uint factionID;

            /// <summary>
            /// Serialized selection state.
            /// </summary>
            public bool selected;

            /// <summary>
            /// Serialized trainer data.
            /// </summary>
            public DtoTrainerData trainerData;

            /// <summary>
            /// Serialized current health value.
            /// </summary>
            public int currentHealth;
        }
    }

    /// <summary>
    /// Data transfer object containing trainer and production queue state data.
    /// </summary>
    [Serializable]
    public struct DtoTrainerData
    {
        /// <summary>
        /// The current unit training progress value.
        /// </summary>
        public float currentProgress;

        /// <summary>
        /// The maximum progress required to complete training.
        /// </summary>
        public float maxProgress;

        /// <summary>
        /// The identifier of the currently training unit.
        /// </summary>
        public string activeUnitKey;

        /// <summary>
        /// Offset applied to the spawn point position.
        /// </summary>
        public Float3Serializable spawnPointOffset;

        /// <summary>
        /// Offset applied to the rally position.
        /// </summary>
        public Float3Serializable rallyPositionOffset;

        /// <summary>
        /// Indicates whether the training queue has changed.
        /// </summary>
        public bool onUnitQueueChange;

        /// <summary>
        /// The queued unit identifiers awaiting training.
        /// </summary>
        public List<string> trainingQueue;

        /// <summary>
        /// Creates a DTO representation from a trainer component and queue buffer.
        /// </summary>
        /// <param name="trainer">The trainer component to serialize.</param>
        /// <param name="queueBuffer">The queued unit buffer associated with the trainer.</param>
        /// <returns>A populated <see cref="DtoTrainerData"/> instance.</returns>
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

        /// <summary>
        /// Converts the DTO into a runtime <see cref="Trainer"/> component.
        /// </summary>
        /// <returns>A populated <see cref="Trainer"/> instance.</returns>
        public Trainer ToTrainer()
        {
            return new Trainer
            {
                currentProgress = currentProgress,
                maxProgress = maxProgress,
                activeUnitKey = new UnitKey
                {
                    name = ParseFixedString64Bytes(activeUnitKey)
                },
                spawnPointOffset = spawnPointOffset.ToFloat3(),
                rallyPositionOffset = rallyPositionOffset.ToFloat3(),
                onUnitQueueChange = onUnitQueueChange
            };
        }

        /// <summary>
        /// Replaces the contents of a queued unit buffer using the stored queue data.
        /// </summary>
        /// <param name="buffer">The dynamic buffer to rewrite.</param>
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
                        name = ParseFixedString64Bytes(key)
                    }
                });
            }
        }

        /// <summary>
        /// Serializes the trainer data to JSON format.
        /// </summary>
        /// <returns>A formatted JSON string representing the trainer data.</returns>
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

        /// <summary>
        /// Deserializes trainer data from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string containing serialized trainer data.</param>
        /// <returns>A populated <see cref="DtoTrainerData"/> instance.</returns>
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

        /// <summary>
        /// Internal serializable representation used for JSON conversion.
        /// </summary>
        [Serializable]
        private struct SerializableTrainerData
        {
            /// <summary>
            /// Serialized training progress value.
            /// </summary>
            public float currentProgress;

            /// <summary>
            /// Serialized maximum training progress value.
            /// </summary>
            public float maxProgress;

            /// <summary>
            /// Serialized active unit identifier.
            /// </summary>
            public string activeUnitKey;

            /// <summary>
            /// Serialized spawn point offset.
            /// </summary>
            public Float3Serializable spawnPointOffset;

            /// <summary>
            /// Serialized rally position offset.
            /// </summary>
            public Float3Serializable rallyPositionOffset;

            /// <summary>
            /// Serialized queue change state.
            /// </summary>
            public bool onUnitQueueChange;

            /// <summary>
            /// Serialized training queue.
            /// </summary>
            public List<string> trainingQueue;
        }
    }
}