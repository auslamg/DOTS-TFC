using System.IO;
using Unity.Mathematics;
using UnityEngine;

public static class SaveLoadUtil
{
    public static void WriteFloat3(BinaryWriter writer, float3 value)
    {
        writer.Write(value.x);
        writer.Write(value.y);
        writer.Write(value.z);
    }

    public static void WriteQuaternion(BinaryWriter writer, quaternion value)
    {
        writer.Write(value.value.x);
        writer.Write(value.value.y);
        writer.Write(value.value.z);
        writer.Write(value.value.w);
    }

    public static float3 ReadFloat3(BinaryReader reader)
    {
        return new float3(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
    }

    public static quaternion ReadQuaternion(BinaryReader reader)
    {
        return new quaternion(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
    }
}
