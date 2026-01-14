using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <c>Health</c> unmanaged component.
/// </summary>
class HealthAuthoring : MonoBehaviour
{
    /// <summary>
    /// Current health points. The entity dies if the value goes 0 or below.
    /// </summary>
    public int currentHealth;
    /// <summary>
    /// Maximum health points. Determins the maximum and default value of <c>currentHealth</c>.
    /// </summary>
    public int maxHealth;
}

/// <summary>
/// Baker for the <c>Health</c> unmanaged component.
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

public struct Health : IComponentData
{
    /// <summary>
    /// Current health points. The entity dies if the value goes 0 or below.
    /// </summary>
    public int currentHealth;
    /// <summary>
    /// Maximum health points. Determins the maximum and default value of <c>currentHealth</c>.
    /// </summary>
    public int maxHealth;
    /// <summary>
    /// Event-bool triggered on <c>currentHealth</c> value change.
    /// </summary>
    /// <remarks>
    /// Custom events are reset at the end of each frame in ResetEventSystem.
    /// </remarks>
    public bool onHealthChanged;
}
