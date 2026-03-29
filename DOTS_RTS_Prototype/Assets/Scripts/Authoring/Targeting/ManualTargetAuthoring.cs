using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="ManualTarget"/> unmanaged component.
/// </summary>
class ManualTargetAuthoring : MonoBehaviour
{

}

/// <summary>
/// Baker for the <see cref="ManualTarget"/> unmanaged component.
/// </summary>
class ManualTargetBaker : Baker<ManualTargetAuthoring>
{
    public override void Bake(ManualTargetAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ManualTarget
        {
        });
    }
}

/// <summary>
/// Used by entities that allow for the manual selection of an individual target. 
/// </summary>
/// <remarks>
/// Requires the <see cref="Targetter"/> component 
/// //IDEA: Enforce implementation through [RequireComponent(typeof(Targetter))]
/// </remarks>
public struct ManualTarget : IComponentData
{
    /// <summary>
    /// Targeted entity.
    /// </summary>
    public Entity targetEntity;
}