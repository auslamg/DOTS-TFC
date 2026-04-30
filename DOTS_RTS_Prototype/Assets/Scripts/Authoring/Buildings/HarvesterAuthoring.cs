using Unity.Entities;
using UnityEngine;

/// <summary>
/// Managed component for the <see cref="Harvester"/> unmanaged component.
/// </summary>
class HarvesterAuthoring : MonoBehaviour
{
    /// <summary>
    /// Key for the resource to be harvested.
    /// </summary>
    [SerializeField]
    [Tooltip("Key of the resource that this spawner should harvest.")]
    public string harvestedResourceKey;

    /// <summary>
    /// Time interval between harvests.
    /// </summary>
    [SerializeField]
    [Tooltip("Time interval between harvests.")]
    public float harvestInterval;

    /// <summary>
    /// Amount of the set resource to harvest every tick.
    /// </summary>
    [SerializeField]
    [Tooltip("Amount of the set resource to harvest every tick.")]
    public int harvestAmount;

    /// <summary>
    /// Maximum distance to a resource source to harvest from it.
    /// </summary>
    [SerializeField]
    [Tooltip("Maximum distance to a resource source to harvest from it.")]
    public float harvestingRange;
}

/// <summary>
/// Baker for the <see cref="Harvester"/> unmanaged component.
/// </summary>
class HarvesterBaker : Baker<HarvesterAuthoring>
{
    public override void Bake(HarvesterAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Harvester
        {
            harvestedResourceKey = new ResourceKey
            {
                name = authoring.harvestedResourceKey
            },
            harvestCooldownTimer = new LoopingTimer
            {
                
                Time = authoring.harvestInterval,
                Interval = authoring.harvestInterval
            },
            harvestAmount = authoring.harvestAmount,
            harvestingRange = authoring.harvestingRange
        });
    }
}

/// <summary>
/// Used for enemy spawn points that generate enemies in a random position in a radius around the <c>LocalTransform</c> position.
/// </summary>
public struct Harvester : IComponentData
{
    /// <summary>
    /// Key for the resrouce to be harvested.
    /// </summary>
    public ResourceKey harvestedResourceKey;

    /// <summary>
    /// Looping timer to wait between unit spawns.
    /// </summary>
    public LoopingTimer harvestCooldownTimer;

    /// <summary>
    /// Amount of the set resource to harvest every tick.
    /// </summary>
    public int harvestAmount;

    /// <summary>
    /// Maximum distance to a resource source to harvest from it.
    /// </summary>
    public float harvestingRange;
}
