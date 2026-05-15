using System;
using System.Collections.Generic;
using System.IO;
using Dto;
using Dto.Buildings;
using Dto.Units;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static SerializationUtil;

/// <summary>
/// Handles serialization and deserialization of game data to and from JSON and binary formats.
/// </summary>
public static class SaveGameSerializer
{
    /// <summary>
    /// Serializes the game data to JSON format.
    /// </summary>
    /// <param name="data">The game data to serialize.</param>
    /// <returns>A JSON string representation of the data.</returns>
    public static string SerializeToJson(DtoGameData data)
    {
        return JsonUtility.ToJson(data, true);
    }

    /// <summary>
    /// Deserializes game data from JSON format.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized game data.</returns>
    public static DtoGameData DeserializeFromJson(string json)
    {
        return JsonUtility.FromJson<DtoGameData>(json);
    }

    /// <summary>
    /// Serializes the game data to binary format and writes it to the specified stream.
    /// </summary>
    /// <param name="data">The game data to serialize.</param>
    /// <param name="stream">The stream to write the binary data to.</param>
    public static void SerializeToBinary(DtoGameData data, Stream stream)
    {
        using BinaryWriter writer = new BinaryWriter(stream);

        // MANAGED
        WriteFloat3(writer, data.managed.camPosition);
        WriteQuaternion(writer, data.managed.camRotation);

        // RESOURCES
        writer.Write(data.resources.resources.Count);
        foreach (var r in data.resources.resources)
        {
            writer.Write(r.resourceKey.name.ToString());
            writer.Write(r.amount);
        }

        // BUILDINGS
        writer.Write(data.buildings.Count);
        foreach (var b in data.buildings)
        {
            writer.Write(b.prefabKey);
            WriteFloat3(writer, b.position);
            WriteQuaternion(writer, b.rotation);
            writer.Write(b.ownerID);
            writer.Write(b.factionID);
            writer.Write(b.selected);
            writer.Write(b.currentHealth);

            bool hasTrainer = b.trainerData.trainingQueue != null;
            writer.Write(hasTrainer);
            if (hasTrainer)
            {
                writer.Write(b.trainerData.currentProgress);
                writer.Write(b.trainerData.maxProgress);
                writer.Write(b.trainerData.activeUnitKey ?? "");
                WriteFloat3(writer, b.trainerData.spawnPointOffset.ToFloat3());
                WriteFloat3(writer, b.trainerData.rallyPositionOffset.ToFloat3());
                writer.Write(b.trainerData.onUnitQueueChange);
                writer.Write(b.trainerData.trainingQueue.Count);
                foreach (var q in b.trainerData.trainingQueue)
                {
                    writer.Write(q);
                }
            }
        }

        // UNITS
        writer.Write(data.units.Count);
        foreach (var u in data.units)
        {
            writer.Write(u.prefabKey);
            WriteFloat3(writer, u.position);
            WriteQuaternion(writer, u.rotation);
            writer.Write(u.ownerID);
            writer.Write(u.factionID);
            writer.Write(u.selected);
            writer.Write(u.requirePathing);
            WriteFloat3(writer, u.unitMoverPosition);
            WriteFloat3(writer, u.targetPosition);
            WriteFloat3(writer, u.postFormationPosition);
            WriteFloat3(writer, u.lastMoveVector);
            WriteEntity(writer, u.targetEntity);
            writer.Write(u.currentHealth);
        }
    }

    /// <summary>
    /// Deserializes game data from binary format from the specified stream.
    /// </summary>
    /// <param name="stream">The stream to read the binary data from.</param>
    /// <returns>The deserialized game data.</returns>
    public static DtoGameData DeserializeFromBinary(Stream stream)
    {
        using BinaryReader reader = new BinaryReader(stream);

        DtoGameData data = new DtoGameData();

        // MANAGED
        data.managed = new DtoManagedData
        {
            camPosition = ReadFloat3(reader),
            camRotation = ReadQuaternion(reader)
        };

        // RESOURCES
        int resourceCount = reader.ReadInt32();
        data.resources = new DtoResourceData
        {
            resources = new List<DtoResourceData.SaveResourceEntry>()
        };
        for (int i = 0; i < resourceCount; i++)
        {
            var entry = new DtoResourceData.SaveResourceEntry
            {
                resourceKey = new ResourceKey { name = reader.ReadString() },
                amount = reader.ReadInt32()
            };
            data.resources.resources.Add(entry);
        }

        // BUILDINGS
        int buildingCount = reader.ReadInt32();
        data.buildings = new List<DtoBuildingData>();
        for (int i = 0; i < buildingCount; i++)
        {
            var building = new DtoBuildingData
            {
                prefabKey = reader.ReadString(),
                position = ReadFloat3(reader),
                rotation = ReadQuaternion(reader),
                ownerID = reader.ReadInt32(),
                factionID = reader.ReadUInt32(),
                selected = reader.ReadBoolean(),
                currentHealth = reader.ReadInt32()
            };

            bool hasTrainer = reader.ReadBoolean();
            if (hasTrainer)
            {
                building.trainerData = new DtoTrainerData
                {
                    currentProgress = reader.ReadSingle(),
                    maxProgress = reader.ReadSingle(),
                    activeUnitKey = reader.ReadString(),
                    spawnPointOffset = new Float3Serializable(ReadFloat3(reader)),
                    rallyPositionOffset = new Float3Serializable(ReadFloat3(reader)),
                    onUnitQueueChange = reader.ReadBoolean()
                };
                int queueCount = reader.ReadInt32();
                building.trainerData.trainingQueue = new List<string>();
                for (int j = 0; j < queueCount; j++)
                {
                    building.trainerData.trainingQueue.Add(reader.ReadString());
                }
            }
            else
            {
                building.trainerData = new DtoTrainerData();
            }

            data.buildings.Add(building);
        }

        // UNITS
        int unitCount = reader.ReadInt32();
        data.units = new List<DtoUnitData>();
        for (int i = 0; i < unitCount; i++)
        {
            var unit = new DtoUnitData
            {
                prefabKey = reader.ReadString(),
                position = ReadFloat3(reader),
                rotation = ReadQuaternion(reader),
                ownerID = reader.ReadInt32(),
                factionID = reader.ReadUInt32(),
                selected = reader.ReadBoolean(),
                requirePathing = reader.ReadBoolean(),
                unitMoverPosition = ReadFloat3(reader),
                targetPosition = ReadFloat3(reader),
                postFormationPosition = ReadFloat3(reader),
                lastMoveVector = ReadFloat3(reader),
                targetEntity = ReadEntity(reader),
                currentHealth = reader.ReadInt32()
            };
            data.units.Add(unit);
        }

        return data;
    }
}