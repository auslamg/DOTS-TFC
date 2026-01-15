using Unity.Entities;
using UnityEngine;

//IDEA: Refactor into "EntitySpawner" with a set Prefab field

/// <summary>
/// Managed component for the <see cref="EnemySpawner"/> unmanaged component.
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
/// Baker for the <see cref="EnemySpawner"/> unmanaged component.
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

//IDEA: Refactor into "EntitySpawner" with a set Prefab field
/// <summary>
/// Used for enemy spawn points that generate enemies in a random position in a radius around the <c>LocalTransform</c> position.
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
