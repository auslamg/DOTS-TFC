using Unity.Entities;
using UnityEngine;

//IDEA: Refactor into "EntitySpawner" with a set Prefab field

/// <summary>
/// Managed component for the <c>EnemySpawner</c> unmanaged component.
/// </summary>
class EnemySpawnerAuthoring : MonoBehaviour
{
    /// <summary>
    /// Time span between spawns.
    /// </summary>
    public float spawnFrequency;
    /// <summary>
    /// Minimum distance from the LocalTransform center for spawn offset.
    /// </summary>
    public float minDistance;
    /// <summary>
    /// Maximum distance from the LocalTransform center for spawn offset.
    /// </summary>
    public float maxDistance;
}

/// <summary>
/// Baker for the <c>EnemySpawner</c> component.
/// </summary>
class EnemySpawnerBaker : Baker<EnemySpawnerAuthoring>
{
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemySpawner
        {
            spawnFrequency = authoring.spawnFrequency,
            minDistance = authoring.minDistance,
            maxDistance = authoring.maxDistance,
        });
    }
}

/// <summary>
/// <c>EnemySpawner</c> entity component.
/// Used for enemy spawn points with random position distribution.
/// Must set values for all fields on instantiation.
/// </summary>
public struct EnemySpawner : IComponentData {
    /// <summary>
    /// Remaining time before next spawn.
    /// </summary>
    public float spawnPhaseTime;
    /// <summary>
    /// Time span between spawns.
    /// </summary>
    public float spawnFrequency;
    /// <summary>
    /// Minimum distance from the LocalTransform center for spawn offset.
    /// </summary>
    public float minDistance;
    /// <summary>
    /// Maximum distance from the LocalTransform center for spawn offset.
    /// </summary>
    public float maxDistance;

}
