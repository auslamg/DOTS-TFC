using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="TargetBuildingSeeker"/> unmanaged component.
/// </summary>
class TargetBuildingSeekerAuthoring : MonoBehaviour
{
    
}

/// <summary>
/// Baker for the <see cref="TargetBuildingSeeker"/> unmanaged component.
/// </summary>
class TargetBuildingSeekerBaker : Baker<TargetBuildingSeekerAuthoring>
{
    public override void Bake(TargetBuildingSeekerAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new TargetBuildingSeeker
        {
            
        });
    }
}

/// <summary>
/// Used by entities that automatically scan for nearby entities to acquire a target whenever there is no target set. 
/// </summary>
/// <remarks>
/// Requires the <see cref="Targetter"/> component 
/// </remarks>
public struct TargetBuildingSeeker : IComponentData
{
    
}