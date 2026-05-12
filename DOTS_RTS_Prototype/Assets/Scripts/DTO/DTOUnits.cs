using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dto.Units
{
    [Serializable]
    public struct DtoUnitData
    {
        public float3 position;
        public quaternion rotation;
        public string prefabKey;
        public int ownerID;
        public uint factionID;

        public bool selected;
        public bool requirePathing;

        public float3 unitMoverPosition;
        public float3 targetPosition;
        public float3 postFormationPosition;
        public float3 lastMoveVector;

        public Entity targetEntity;
        public int currentHealth;

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

        [Serializable]
        private struct SerializableUnitData
        {
            public Float3Serializable position;
            public QuaternionSerializable rotation;

            public string prefabKey;
            public int unitOwner;
            public uint factionID;

            public bool selected;
            public bool requirePathing;

            public Float3Serializable unitMoverPosition;
            public Float3Serializable movePosition;
            public Float3Serializable postFormationPosition;
            public Float3Serializable lastMoveVector;

            public EntitySerializable targetEntity;
            public int currentHealth;
        }
    }
}
