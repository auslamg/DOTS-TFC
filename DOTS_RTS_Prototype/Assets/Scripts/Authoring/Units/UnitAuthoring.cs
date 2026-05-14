using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="Unit"/> unmanaged component.
/// </summary>
public class UnitAuthoring : MonoBehaviour
{
    /// <summary>
    /// Owning player/faction identifier for this unit.
    /// </summary>
    [SerializeField]
    [Tooltip("Owning player or faction identifier for this unit.")]
    public int ownerID;
}

/// <summary>
/// Baker for the <see cref="Unit"/> unmanaged component.
/// </summary>
class UnitBaker : Baker<UnitAuthoring>
{
    public override void Bake(UnitAuthoring authoring)
    {
        float colliderOffset = GetOffset(authoring);

        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Unit
        {
            ownerID = authoring.ownerID,
            colliderOffsetRadius = colliderOffset,
        });
    }

    /// <summary>
    /// Calculates the collision offset radius for a unit based on its attached collider.
    /// </summary>
    /// <param name="authoring">
    /// The unit authoring component containing collider data.
    /// </param>
    /// <returns>
    /// The calculated offset value derived from the detected collider type.
    /// Returns <c>0f</c> if no supported collider is found.
    /// </returns>
    private float GetOffset(UnitAuthoring authoring)
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
                math.pow(authoring.GetComponent<BoxCollider>().size.y, 2)
            );
        }

        return 0f;
    }
}

/// <summary>
/// Used by entities that represent a unit.
/// </summary>
public struct Unit : IComponentData
{
    /// <summary>
    /// Owning player/faction identifier for this unit.
    /// </summary>
    public int ownerID;
    /// <summary>
    /// Radius-like collider offset used by proximity calculations.
    /// </summary>
    public float colliderOffsetRadius;
}