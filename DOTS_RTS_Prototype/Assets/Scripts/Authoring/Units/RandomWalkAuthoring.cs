using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="RandomWalk"/> unmanaged component.
/// </summary>
class RandomWalkAuthoring : MonoBehaviour
{
    /// <summary>
    /// Current desired position to move the Entity to.
    /// </summary>
    [SerializeField]
    [Tooltip("Current desired destination for random walk movement.")]
    public float3 targetPostion;
    /// <summary>
    /// Center point for generating the random walking destinations.
    /// </summary>
    [SerializeField]
    [Tooltip("Origin point used as center for random destination generation.")]
    public float3 originPointPosition;
    /// <summary>
    /// Minimum distance from the origin point for randomly generated destinations.
    /// </summary>
    [SerializeField]
    [Tooltip("Minimum distance from origin when generating random destinations.")]
    public float minDistance;
    /// <summary>
    /// Maximum distance from the origin point for randomly generated destinations.
    /// </summary>
    [SerializeField]
    [Tooltip("Maximum distance from origin when generating random destinations.")]
    public float maxDistance;
    /// <summary>
    /// Seed value used to generate deterministic random destinations.
    /// </summary>
    /// <remarks>
    /// If no seed is provided, a value derived from the entity index is used.
    /// Results remain deterministic as long as entity creation order is deterministic.
    /// </remarks>
    [SerializeField]
    [Tooltip("Seed used for deterministic random destination generation. Use 0 to auto-generate from entity index.")]
    public uint randomSeed;
}

/// <summary>
/// Baker for the <see cref="RandomWalk"/> unmanaged component.
/// </summary>
class RandomWalkBaker : Baker<RandomWalkAuthoring>
{
    public override void Bake(RandomWalkAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new RandomWalk
        {
            targetPostion = authoring.targetPostion,
            originPointPosition = authoring.originPointPosition,
            minDistance = authoring.minDistance,
            maxDistance = authoring.maxDistance,
            random = new Unity.Mathematics.Random(
                authoring.randomSeed == 0 ?
                    (uint)entity.Index :
                    authoring.randomSeed)
        });
    }
}

/// <summary>
/// Used by entities that automatically generate random move destinations in a set radius around an origin position whenever there is no destination set.
/// </summary>
/// <remarks>
/// Requires the <see cref="UnitMover"/> component 
/// </remarks>
public struct RandomWalk : IComponentData
{
    /// <summary>
    /// Current desired position to move the Entity to.
    /// </summary>
    public float3 targetPostion;
    /// <summary>
    /// Center point for generating the random walking destinations.
    /// </summary>
    public float3 originPointPosition;
    /// <summary>
    /// Minimum distance from the origin point for randomly generated destinations.
    /// </summary>
    public float minDistance;
    /// <summary>
    /// Maximum distance from the origin point for randomly generated destinations.
    /// </summary>
    public float maxDistance;
    /// <summary>
    /// Random number generator.
    /// </summary>
    /// <remarks>
    /// Generated numbers are deterministic based on the managed randomSeed field.
    /// </remarks>
    public Unity.Mathematics.Random random;
}
