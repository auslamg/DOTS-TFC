using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="UnitDataSOHolder"/> unmanaged component.
/// </summary>
public class UnitDataSOHolderAuthoring : MonoBehaviour
{
    /// <summary>
    /// Name portion of the <see cref="UnitKey"/> used for lookup.
    /// </summary>
    [SerializeField]
    [Tooltip("Name portion of the UnitKey used for unit data lookup.")]
    public string unitKeyName;
    /// <summary>
    /// Unit type metadata paired with <see cref="unitKeyName"/>.
    /// </summary>
    [SerializeField]
    [Tooltip("Unit type metadata paired with the unit key name.")]
    public UnitType unitKeyType;
}

/// <summary>
/// Baker for the <see cref="UnitDataSOHolder"/> unmanaged component.
/// </summary>
class UnitDataSOHolderBaker : Baker<UnitDataSOHolderAuthoring>
{
    public override void Bake(UnitDataSOHolderAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new UnitDataSOHolder
        {
            unitKey = new UnitKey{
                name = authoring.unitKeyName
            }
        });
    }

}

/// <summary>
/// Used to pass down a reference to the Unit ScriptableObject.
/// </summary>
public struct UnitDataSOHolder : IComponentData
{
    /// <summary>
    /// Lookup key used to resolve a unit entry in runtime registries.
    /// </summary>
    public UnitKey unitKey;
}