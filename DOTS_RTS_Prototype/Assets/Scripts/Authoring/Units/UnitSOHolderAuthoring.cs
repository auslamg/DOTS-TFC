using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="UnitSOHolder"/> unmanaged component.
/// </summary>
public class UnitSOHolderAuthoring : MonoBehaviour
{
    public string unitKeyName;
    public UnitType unitKeyType;
}

/// <summary>
/// Baker for the <see cref="UnitSOHolder"/> unmanaged component.
/// </summary>
class UnitSOHolderBaker : Baker<UnitSOHolderAuthoring>
{
    public override void Bake(UnitSOHolderAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new UnitSOHolder
        {
            unitKey = new UnitKey{
                name = authoring.unitKeyName,
                unitType = authoring.unitKeyType
            }
        });
    }

}

/// <summary>
/// Used to pass down a reference to the Unit ScriptableObject.
/// </summary>
public struct UnitSOHolder : IComponentData
{
    public UnitKey unitKey;
}