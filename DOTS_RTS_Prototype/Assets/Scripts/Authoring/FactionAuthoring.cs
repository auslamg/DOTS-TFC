using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Managed component for the <c>Faction</c> unmanaged component.
/// </summary>
class FactionAuthoring : MonoBehaviour
{
    /// <summary>
    /// Faction identifier number.
    /// </summary>
    public uint factionID;
}

/// <summary>
/// Baker for the <c>Faction</c> component.
/// </summary>
class FactionBaker : Baker<FactionAuthoring>
{
    public override void Bake(FactionAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Faction
        {
            factionID = authoring.factionID,
        });
    }
}

/// <summary>
/// <c>Faction</c> entity component.
/// Used for unit ownership identification.
/// </summary>
public struct Faction : IComponentData
{
    /// <summary>
    /// Faction identifier number.
    /// </summary>
    public uint factionID;
}
