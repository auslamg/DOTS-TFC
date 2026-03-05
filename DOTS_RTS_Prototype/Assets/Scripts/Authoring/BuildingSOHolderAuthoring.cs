using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="BuildingSOHolder"/> unmanaged component.
/// </summary>
public class BuildingSOHolderAuthoring : MonoBehaviour
{
    public string buildingKeyName;
    public BuildingType buildingKeyType;
}

/// <summary>
/// Baker for the <see cref="BuildingSOHolder"/> unmanaged component.
/// </summary>
class BuildingSOHolderBaker : Baker<BuildingSOHolderAuthoring>
{
    public override void Bake(BuildingSOHolderAuthoring authoring)
    {
        

        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new BuildingSOHolder
        {
            buildingKey = new BuildingKey{
                name = authoring.buildingKeyName,
                buildingType = authoring.buildingKeyType
            }
        });
    }

}

/// <summary>
/// Used to pass down a reference to the Building ScriptableObject.
/// </summary>
public struct BuildingSOHolder : IComponentData
{
    public BuildingKey buildingKey;
}