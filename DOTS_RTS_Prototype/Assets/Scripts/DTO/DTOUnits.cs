using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dto.Units
{
    /// <summary>
    /// Represents serialized unit state data.
    /// </summary>
    [Serializable]
    public struct DtoUnitData
    {
        /// <summary>
        /// The unit world position.
        /// </summary>
        public float3 position;

        /// <summary>
        /// The unit world rotation.
        /// </summary>
        public quaternion rotation;

        /// <summary>
        /// The prefab identifier used to recreate the unit.
        /// </summary>
        public string prefabKey;

        /// <summary>
        /// The owning player identifier.
        /// </summary>
        public int ownerID;

        /// <summary>
        /// The faction identifier.
        /// </summary>
        public uint factionID;

        /// <summary>
        /// Indicates whether the unit is currently selected.
        /// </summary>
        public bool selected;

        /// <summary>
        /// Indicates whether the unit currently requires pathfinding.
        /// </summary>
        public bool requirePathing;

        /// <summary>
        /// The current unit mover position.
        /// </summary>
        public float3 unitMoverPosition;

        /// <summary>
        /// The current movement target position.
        /// </summary>
        public float3 targetPosition;

        /// <summary>
        /// The assigned post-formation position.
        /// </summary>
        public float3 postFormationPosition;

        /// <summary>
        /// The last recorded movement direction vector.
        /// </summary>
        public float3 lastMoveVector;

        /// <summary>
        /// The current target entity.
        /// </summary>
        public Entity targetEntity;

        /// <summary>
        /// The unit's current health value.
        /// </summary>
        public int currentHealth;

        /// <summary>
        /// Returns a formatted string representation of the unit data.
        /// </summary>
        /// <returns>A formatted unit data string.</returns>
        public override string ToString()
        {
            return $"UnitSaveData(" +
                   $"prefabKey: {prefabKey}, " +
                   $"ownerID: {ownerID}, " +
                   $"factionID: {factionID}, " +
                   $"selected: {selected}, " +
                   $"requirePathing: {requirePathing}, " +
                   $"position: ({position.x}, {position.y}, {position.z}), " +
                   $"unitMoverPosition: ({unitMoverPosition.x}, {unitMoverPosition.y}, {unitMoverPosition.z}), " +
                   $"rotation: ({rotation.value.x}, {rotation.value.y}, {rotation.value.z}, {rotation.value.w}), " +
                   $"movePosition: ({targetPosition.x}, {targetPosition.y}, {targetPosition.z}), " +
                   $"postFormationPosition: ({postFormationPosition.x}, {postFormationPosition.y}, {postFormationPosition.z}), " +
                   $"lastMoveVector: ({lastMoveVector.x}, {lastMoveVector.y}, {lastMoveVector.z}), " +
                   $"targetEntity: (Index: {targetEntity.Index}, Version: {targetEntity.Version}), " +
                   $"currentHealth: {currentHealth})";
        }

        /// <summary>
        /// Serializes the unit data into JSON format.
        /// </summary>
        /// <returns>A formatted JSON string.</returns>
        public string ToJson()
        {
            var serializable = new SerializableUnitData
            {
                position = new Float3Serializable(position),
                rotation = new QuaternionSerializable(rotation),
                prefabKey = prefabKey,
                unitOwner = ownerID,
                factionID = factionID,

                selected = selected,
                requirePathing = requirePathing,

                unitMoverPosition = new Float3Serializable(unitMoverPosition),
                movePosition = new Float3Serializable(targetPosition),
                postFormationPosition = new Float3Serializable(postFormationPosition),
                lastMoveVector = new Float3Serializable(lastMoveVector),

                targetEntity = new EntitySerializable(targetEntity),
                currentHealth = currentHealth,
            };

            return JsonUtility.ToJson(serializable, true);
        }

        /// <summary>
        /// Deserializes unit data from JSON.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns>A populated <see cref="DtoUnitData"/> instance.</returns>
        public static DtoUnitData FromJson(string json)
        {
            var data = JsonUtility.FromJson<SerializableUnitData>(json);

            return new DtoUnitData
            {
                position = new float3(data.position.x, data.position.y, data.position.z),
                rotation = new quaternion(data.rotation.x, data.rotation.y, data.rotation.z, data.rotation.w),

                prefabKey = data.prefabKey,
                ownerID = data.unitOwner,
                factionID = data.factionID,

                selected = data.selected,
                requirePathing = data.requirePathing,

                unitMoverPosition = new float3(
                    data.unitMoverPosition.x,
                    data.unitMoverPosition.y,
                    data.unitMoverPosition.z),

                targetPosition = new float3(
                    data.movePosition.x,
                    data.movePosition.y,
                    data.movePosition.z),

                postFormationPosition = new float3(
                    data.postFormationPosition.x,
                    data.postFormationPosition.y,
                    data.postFormationPosition.z),

                lastMoveVector = new float3(
                    data.lastMoveVector.x,
                    data.lastMoveVector.y,
                    data.lastMoveVector.z),

                targetEntity = new Entity
                {
                    Index = data.targetEntity.index,
                    Version = data.targetEntity.version
                },

                currentHealth = data.currentHealth,
            };
        }

        /// <summary>
        /// Internal serializable representation of unit data.
        /// </summary>
        [Serializable]
        private struct SerializableUnitData
        {
            /// <summary>
            /// Serialized unit position.
            /// </summary>
            public Float3Serializable position;

            /// <summary>
            /// Serialized unit rotation.
            /// </summary>
            public QuaternionSerializable rotation;

            /// <summary>
            /// Serialized prefab identifier.
            /// </summary>
            public string prefabKey;

            /// <summary>
            /// Serialized owner identifier.
            /// </summary>
            public int unitOwner;

            /// <summary>
            /// Serialized faction identifier.
            /// </summary>
            public uint factionID;

            /// <summary>
            /// Serialized selection state.
            /// </summary>
            public bool selected;

            /// <summary>
            /// Serialized pathfinding requirement state.
            /// </summary>
            public bool requirePathing;

            /// <summary>
            /// Serialized mover position.
            /// </summary>
            public Float3Serializable unitMoverPosition;

            /// <summary>
            /// Serialized movement target position.
            /// </summary>
            public Float3Serializable movePosition;

            /// <summary>
            /// Serialized formation position.
            /// </summary>
            public Float3Serializable postFormationPosition;

            /// <summary>
            /// Serialized movement vector.
            /// </summary>
            public Float3Serializable lastMoveVector;

            /// <summary>
            /// Serialized target entity reference.
            /// </summary>
            public EntitySerializable targetEntity;

            /// <summary>
            /// Serialized health value.
            /// </summary>
            public int currentHealth;
        }
    }
}