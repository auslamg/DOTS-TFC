using System;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Provides low-level binary serialization helpers for ECS types and Unity mathematics values.
/// </summary>
public static class SerializationUtil
{
    /// <summary>
    /// Writes a <see cref="float3"/> value into a binary stream as three single-precision floats.
    /// </summary>
    /// <param name="writer">The binary writer used to write data.</param>
    /// <param name="value">The float3 value to serialize.</param>
    public static void WriteFloat3(BinaryWriter writer, float3 value)
    {
        writer.Write(value.x);
        writer.Write(value.y);
        writer.Write(value.z);
    }

    /// <summary>
    /// Writes a <see cref="quaternion"/> value into a binary stream as four single-precision floats.
    /// </summary>
    /// <param name="writer">The binary writer used to write data.</param>
    /// <param name="value">The quaternion value to serialize.</param>
    public static void WriteQuaternion(BinaryWriter writer, quaternion value)
    {
        writer.Write(value.value.x);
        writer.Write(value.value.y);
        writer.Write(value.value.z);
        writer.Write(value.value.w);
    }

    /// <summary>
    /// Reads a <see cref="float3"/> value from a binary stream.
    /// </summary>
    /// <param name="reader">The binary reader used to read data.</param>
    /// <returns>The deserialized float3 value.</returns>
    public static float3 ReadFloat3(BinaryReader reader)
    {
        return new float3(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
    }

    /// <summary>
    /// Reads a <see cref="quaternion"/> value from a binary stream.
    /// </summary>
    /// <param name="reader">The binary reader used to read data.</param>
    /// <returns>The deserialized quaternion value.</returns>
    public static quaternion ReadQuaternion(BinaryReader reader)
    {
        return new quaternion(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
    }

    /// <summary>
    /// Writes an ECS <see cref="Entity"/> reference into a binary stream by serializing its index and version.
    /// </summary>
    /// <param name="writer">The binary writer used to write data.</param>
    /// <param name="entity">The entity reference to serialize.</param>
    public static void WriteEntity(BinaryWriter writer, Entity entity)
    {
        writer.Write(entity.Index);
        writer.Write(entity.Version);
    }

    /// <summary>
    /// Reads an ECS <see cref="Entity"/> reference from a binary stream.
    /// </summary>
    /// <param name="reader">The binary reader used to read data.</param>
    /// <returns>The deserialized entity reference.</returns>
    public static Entity ReadEntity(BinaryReader reader)
    {
        return new Entity
        {
            Index = reader.ReadInt32(),
            Version = reader.ReadInt32()
        };
    }

    /// <summary>
    /// Converts a regular string into a <see cref="FixedString64Bytes"/> value in a safe way.
    /// </summary>
    /// <param name="value">The source string value.</param>
    /// <returns>A <see cref="FixedString64Bytes"/> representation of the string that fits the size limit.</returns>
    public static FixedString64Bytes ParseFixedString64Bytes(string value)
    {
        if (string.IsNullOrEmpty(value))
            return default;

        try
        {
            return new FixedString64Bytes(value);
        }
        catch (ArgumentException)
        {
            int length = value.Length;
            while (length > 0)
            {
                try
                {
                    return new FixedString64Bytes(value.Substring(0, length));
                }
                catch (ArgumentException)
                {
                    length--;
                }
            }

            return default;
        }
    }
}
