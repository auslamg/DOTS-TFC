using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="BuildingDataSOHolder"/> unmanaged component.
/// </summary>
public class BuildingDataSOHolderAuthoring : MonoBehaviour
{
    public string buildingKeyName;
    public BuildingType buildingKeyType;
}

/// <summary>
/// Baker for the <see cref="BuildingDataSOHolder"/> unmanaged component.
/// </summary>
class BuildingDataSOHolderBaker : Baker<BuildingDataSOHolderAuthoring>
{
    public override void Bake(BuildingDataSOHolderAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new BuildingDataSOHolder
        {
            buildingKey = new BuildingKey{
                name = authoring.buildingKeyName
            }
        });
    }

}

/// <summary>
/// Used to pass down a reference to the Building ScriptableObject.
/// </summary>
public struct BuildingDataSOHolder : IComponentData
{
    public BuildingKey buildingKey;
}