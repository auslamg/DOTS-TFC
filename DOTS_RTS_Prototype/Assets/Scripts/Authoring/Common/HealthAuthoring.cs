using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="Health"/> unmanaged component.
/// </summary>
class HealthAuthoring : MonoBehaviour
{
    /// <summary>
    /// Current health points. The entity dies if the value goes 0 or below.
    /// </summary>
    [SerializeField]
    [Tooltip("Current health points for this entity.")]
    public int currentHealth;
    /// <summary>
    /// Maximum health points. Determines the maximum and default value of <c>currentHealth</c>.
    /// </summary>
    [SerializeField]
    [Tooltip("Maximum health points for this entity.")]
    public int maxHealth;
}

/// <summary>
/// Baker for the <see cref="Health"/> unmanaged component.
/// </summary>
class HealthBaker : Baker<HealthAuthoring>
{
    public override void Bake(HealthAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Health
        {
            currentHealth = authoring.currentHealth,
            maxHealth = authoring.maxHealth,
            onHealthChanged = true
        });
    }
}

/// <summary>
/// Used to represent health for units that can receive damage.
/// </summary>
public struct Health : IComponentData
{
    /// <summary>
    /// Current health points. The entity dies if the value goes 0 or below.
    /// </summary>
    public int currentHealth;
    /// <summary>
    /// Maximum health points. Determines the maximum and default value of <c>currentHealth</c>.
    /// </summary>
    public int maxHealth;
    /// <summary>
    /// Event-bool triggered on <c>currentHealth</c> value change.
    /// </summary>
    /// <remarks>
    /// Custom events are reset at the end of each frame in ResetEventSystem.
    /// </remarks>
    public bool onHealthChanged;
    /// <summary>
    /// Event-bool triggered when health reaches zero or below.
    /// </summary>
    public bool onDeath;
}
