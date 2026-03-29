using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="UnitMover"/> unmanaged component.
/// </summary>
public class UnitMoverAuthoring : MonoBehaviour
{
    /// <summary>
    /// Movement speed in meters/second.
    /// </summary>
    [SerializeField]
    [Tooltip("Movement speed in meters per second.")]
    public float moveSpeed = 10f;
    /// <summary>
    /// Rotation speed in radians/second. Currently rotation is visual-only and does not affect pathfinding or aiming.
    /// </summary>
    [SerializeField]
    [Tooltip("Rotation speed in radians per second.")]
    public float rotationSpeed = 5f;
    /// <summary>
    /// Maximum distance between this entity and its target before arrival is considered complete.
    /// </summary>
    [SerializeField]
    [Tooltip("Arrival threshold distance to consider the movement target reached.")]
    public float targetReachedDistanceSquared = 2f;
}

/// <summary>
/// Baker for the <see cref="UnitMover"/> unmanaged component.
/// </summary>
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

/// <summary>
/// Used by unit entities that gradually move across the scene with set destinations.
/// </summary>
/// <remarks>
/// Requires the <see cref="Unit"/> component 
/// //IDEA: Enforce implementation through [RequireComponent(typeof(Unit))]
/// </remarks>
public struct UnitMover : IComponentData
{
    /// <summary>
    /// Movement speed in meters/second.
    /// </summary>
    public float moveSpeed;
    /// <summary>
    /// Rotation speed in radians/second. Currently rotation is visual-only and does not affect pathfinding or aiming.
    /// </summary>
    public float rotationSpeed;
    /// <summary>
    /// Maximum distance between this entity and its target before arrival is considered complete.
    /// </summary>
    public float targetReachedDistanceSquared;
    /// <summary>
    /// Current desired position to move the Entity to.
    /// </summary>
    public float3 targetPosition;
    /// <summary>
    /// Determines whether the entity is currently moving. //TODO: Use for FSM
    /// </summary>
    public bool isMoving;
}

