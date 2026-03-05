using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="Building"/> unmanaged component.
/// </summary>
//TODO: Implement owners with ID's as Data
public class BuildingAuthoring : MonoBehaviour
{
    public int ownerID;
}

/// <summary>
/// Baker for the <see cref="Building"/> unmanaged component.
/// </summary>
class BuildingBaker : Baker<BuildingAuthoring>
{
    public override void Bake(BuildingAuthoring authoring)
    {
        float colliderOffset = GetOffset(authoring);

        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Building
        {
            ownerID = authoring.ownerID,
            colliderOffsetRadius = colliderOffset,
        });
    }

    private float GetOffset(BuildingAuthoring authoring)
    {
        if (authoring.GetComponent<CapsuleCollider>() != null)
        {
            return authoring.GetComponent<CapsuleCollider>().radius;
        }
        if (authoring.GetComponent<SphereCollider>() != null)
        {
            return authoring.GetComponent<SphereCollider>().radius;
        }
        if (authoring.GetComponent<BoxCollider>() != null)
        {
            return math.sqrt(
                math.pow(authoring.GetComponent<BoxCollider>().size.x, 2) +
                math.pow(authoring.GetComponent<BoxCollider>().size.z, 2)
                );
        }
        return 0f;
    }
}

/// <summary>
/// Used by entities that represent a building.
/// </summary>
public struct Building : IComponentData
{
    public int ownerID;
    public float colliderOffsetRadius;
}