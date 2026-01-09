using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <c>UnitMover</c> unmanaged component.
/// </summary>
public class UnitMoverAuthoring : MonoBehaviour
{
    /// <summary>
    /// Movement speed in meters/second.
    /// </summary>
    public float moveSpeed = 10f;
    /// <summary>
    /// Rotation speed in rads/second. Currently rotation is merely aesthetic and does not impact pathafinding or aiming.
    /// </summary>
    public float rotationSpeed = 5f;
    /// <summary>
    /// Maximum distance between this entity and target to consider the target reached.
    /// </summary>
    public float targetReachedDistanceSquared = 2f;
}

public class UnitMoverBaker : Baker<UnitMoverAuthoring>
{
    public override void Bake(UnitMoverAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new UnitMover
        {
            moveSpeed = authoring.moveSpeed,
            rotationSpeed = authoring.rotationSpeed,
            targetReachedDistanceSquared = authoring.targetReachedDistanceSquared,
            targetPosition = authoring.transform.position
        });
    }
}

public struct UnitMover : IComponentData
{
    /// <summary>
    /// Movement speed in meters/second.
    /// </summary>
    public float moveSpeed;
    /// <summary>
    /// Rotation speed in rads/second. Currently rotation is merely aesthetic and does not impact pathafinding or aiming.
    /// </summary>
    public float rotationSpeed;
    /// <summary>
    /// Maximum distance between this entity and target to consider the target reached.
    /// </summary>
    public float targetReachedDistanceSquared;
    /// <summary>
    /// Current desired position to move the Entity to.
    /// </summary>
    public float3 targetPosition;
    /// <summary>
    /// Determins if the entity is currently moving. //TODO: Use for FSM
    /// </summary>
    public bool isMoving;
}

