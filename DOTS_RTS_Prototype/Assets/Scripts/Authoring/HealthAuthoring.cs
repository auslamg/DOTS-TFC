using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <c>Health</c> unmanaged component.
/// </summary>
class HealthAuthoring : MonoBehaviour
{
    public int currentHealth;
    public int maxHealth;
}

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
    public int currentHealth;
    public int maxHealth;
    public bool onHealthChanged;
}
