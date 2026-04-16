using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Managed component for the <see cref="Faction"/> unmanaged component.
/// </summary>
class FactionAuthoring : MonoBehaviour
{
    /// <summary>
    /// Faction identifier number.
    /// </summary>
    [SerializeField]
    [Tooltip("Faction identifier assigned to this entity.")]
    public uint factionID;
}

/// <summary>
/// Baker for the <see cref="Faction"/> unmanaged component.
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
/// Used for unit ownership identification.
/// </summary>
public struct Faction : IComponentData
{
    /// <summary>
    /// Faction identifier number.
    /// </summary>
    public uint factionID;
}
