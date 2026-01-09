using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <c>RandomWalk</c> unmanaged component.
/// </summary>
class RandomWalkAuthoring : MonoBehaviour
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
    /// Seed number employed to generate positions. The "randomly" generated positions will be entirely deterministic based on the seed given.
    /// </summary>
    /// /// <remarks>
    /// If the entity isn't given a specific seed, it will generate a new one based on its Entity index. The Entity index is unique to each concurrently existing entity, but .If the same entity types appear at the exact same order, determinism still applies.
    /// </remarks>
    public uint randomSeed;
}

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
