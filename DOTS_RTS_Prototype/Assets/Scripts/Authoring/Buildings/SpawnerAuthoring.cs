using Unity.Entities;
using UnityEngine;

/// <summary>
/// Managed component for the <see cref="Spawner"/> unmanaged component.
/// </summary>
class SpawnerAuthoring : MonoBehaviour
{
    /// <summary>
    /// Key for the entity to be spawned.
    /// </summary>
    [SerializeField]
    [Tooltip("Prefab key of the entity that this spawner should spawn.")]
    public string spawnedEntityKey;
    /// <summary>
    /// Time interval between spawns.
    /// </summary>
    [SerializeField]
    [Tooltip("Time interval between spawn attempts.")]
    public float spawnFrequency;
    /// <summary>
    /// Minimum distance from the LocalTransform center for spawn offset.
    /// </summary>
    [SerializeField]
    [Tooltip("Minimum spawn offset distance from the spawner center.")]
    public float minDistance;
    /// <summary>
    /// Maximum distance from the LocalTransform center for spawn offset.
    /// </summary>
    [SerializeField]
    [Tooltip("Maximum spawn offset distance from the spawner center.")]
    public float maxDistance;
    /// <summary>
    /// Maximum value for nearby scanned enemies to keep spawning more.
    /// </summary>
    [SerializeField]
    [Tooltip("Maximum number of nearby entities allowed before spawns are blocked.")]
    public int nearbyEntityCap;
    /// <summary>
    /// Radius for scanning around for nearby enemies in order to check if the cap was reached.
    /// </summary>
    [SerializeField]
    [Tooltip("Scan radius used to count nearby entities for spawn cap checks.")]
    public float nearbyEntityScanRadius;
}

/// <summary>
/// Baker for the <see cref="Spawner"/> unmanaged component.
/// </summary>
class SpawnerBaker : Baker<SpawnerAuthoring>
{
    public override void Bake(SpawnerAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Spawner
        {
            spawnedEntityKey = new EntityPrefabKey
            {
                name = authoring.spawnedEntityKey
            },
            spawnFrequency = authoring.spawnFrequency,
            minDistance = authoring.minDistance,
            maxDistance = authoring.maxDistance,
            nearbyEntityCap = authoring.nearbyEntityCap,
            nearbyEntityScanRadius = authoring.nearbyEntityScanRadius
        });
    }
}

/// <summary>
/// Used for enemy spawn points that generate enemies in a random position in a radius around the <c>LocalTransform</c> position.
/// </summary>
public struct Spawner : IComponentData
{
    /// <summary>
    /// Key for the entity to be spawned.
    /// </summary>
    public EntityPrefabKey spawnedEntityKey;
    /// <summary>
    /// Remaining time before next spawn.
    /// </summary>
    public float spawnPhaseTime;
    /// <summary>
    /// Time interval between spawns.
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
    /// <summary>
    /// Maximum value for nearby scanned entities to keep spawning more.
    /// </summary>
    public int nearbyEntityCap;
    /// <summary>
    /// Radius for scanning around for nearby entities in order to check if the cap was reached.
    /// </summary>
    public float nearbyEntityScanRadius;

}
