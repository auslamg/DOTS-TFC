using Unity.Entities;
using UnityEngine;

//REVIEW: healthbar can be refactored into being a base unit component or a bar component instead of extensive reference, sacrificing project clarity for performance.
/// <summary>
/// Managed component for the <see cref="HealthBar"/> unmanaged component.
/// </summary>
class HealthBarAuthoring : MonoBehaviour
{
    /// <summary>
    /// Child object that renders the fill visuals of the bar.
    /// </summary>
    [SerializeField]
    [Tooltip("GameObject that renders the fill portion of the health bar.")]
    public GameObject visualBarGameObject;
    /// <summary>
    /// Entity whose health values are represented by this bar.
    /// </summary>
    [SerializeField]
    [Tooltip("GameObject converted to the entity whose Health drives this bar.")]
    public GameObject healthGameObject;
}

/// <summary>
/// Baker for the <see cref="HealthBar"/> unmanaged component.
/// </summary>
class HealthBarBaker : Baker<HealthBarAuthoring>
{
    public override void Bake(HealthBarAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new HealthBar
        {
            visualBarEntity = GetEntity(authoring.visualBarGameObject, TransformUsageFlags.NonUniformScale),
            healthEntity = GetEntity(authoring.healthGameObject, TransformUsageFlags.Dynamic)
        });
    }
}

/// <summary>
/// Used by entities that contain a healthbar, representing the health of a different entity (usually a parent).
/// </summary>
public struct HealthBar : IComponentData
{
    /// <summary>
    /// Entity rendering the fill visuals of the health bar.
    /// </summary>
    public Entity visualBarEntity;
    /// <summary>
    /// Entity whose health values drive this health bar.
    /// </summary>
    public Entity healthEntity;
}
