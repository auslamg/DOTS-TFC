using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="UnitDataSOHolder"/> unmanaged component.
/// </summary>
public class UnitDataSOHolderAuthoring : MonoBehaviour
{
    public string unitKeyName;
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
    public UnitKey unitKey;
}