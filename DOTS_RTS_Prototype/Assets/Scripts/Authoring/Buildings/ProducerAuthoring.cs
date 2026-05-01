using Unity.Entities;
using UnityEngine;

/// <summary>
/// Managed component for the <see cref="Producer"/> unmanaged component.
/// </summary>
class ProducerAuthoring : MonoBehaviour
{
    /// <summary>
    /// Key for the resource to be produced.
    /// </summary>
    [SerializeField]
    [Tooltip("Key of the resource that this spawner should produc.")]
    public string producedResourceKey;

    /// <summary>
    /// Time interval between producs.
    /// </summary>
    [SerializeField]
    [Tooltip("Time interval between producs.")]
    public float productionInterval;

    /// <summary>
    /// Amount of the set resource to produc every tick.
    /// </summary>
    [SerializeField]
    [Tooltip("Amount of the set resource to produc every tick.")]
    public int productionAmount;
}

/// <summary>
/// Baker for the <see cref="Producer"/> unmanaged component.
/// </summary>
class ProducerBaker : Baker<ProducerAuthoring>
{
    public override void Bake(ProducerAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Producer
        {
            producedResourceKey = new ResourceKey
            {
                name = authoring.producedResourceKey
            },
            productionCooldownTimer = new LoopingTimer
            {

                Time = authoring.productionInterval,
                Interval = authoring.productionInterval
            },
            productionAmount = authoring.productionAmount,
        });
    }
}

/// <summary>
/// Used for enemy spawn points that generate enemies in a random position in a radius around the <c>LocalTransform</c> position.
/// </summary>
public struct Producer : IComponentData
{
    /// <summary>
    /// Key for the resrouce to be produced.
    /// </summary>
    public ResourceKey producedResourceKey;

    /// <summary>
    /// Looping timer to wait between unit spawns.
    /// </summary>
    public LoopingTimer productionCooldownTimer;

    /// <summary>
    /// Amount of the set resource to produced every tick.
    /// </summary>
    public int productionAmount;
}
