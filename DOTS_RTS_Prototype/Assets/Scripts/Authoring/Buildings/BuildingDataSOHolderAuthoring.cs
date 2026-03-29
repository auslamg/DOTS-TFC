using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="BuildingDataSOHolder"/> unmanaged component.
/// </summary>
public class BuildingDataSOHolderAuthoring : MonoBehaviour
{
    /// <summary>
    /// Name portion of the <see cref="BuildingKey"/> used for lookup.
    /// </summary>
    [SerializeField]
    [Tooltip("Name portion of the BuildingKey used for building data lookup.")]
    public string buildingKeyName;
    /// <summary>
    /// Building type metadata paired with <see cref="buildingKeyName"/>.
    /// </summary>
    [SerializeField]
    [Tooltip("Building type metadata paired with the building key name.")]
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
            },
            buildingKeyType = authoring.buildingKeyType
        });
    }

}

/// <summary>
/// Used to pass down a reference to the Building ScriptableObject.
/// </summary>
public struct BuildingDataSOHolder : IComponentData
{
    /// <summary>
    /// Lookup key used to resolve a building entry in runtime registries.
    /// </summary>
    public BuildingKey buildingKey;
    /// <summary>
    /// Building type metadata paired with the lookup key.
    /// </summary>
    public BuildingType buildingKeyType;

}