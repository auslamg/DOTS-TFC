using Unity.Entities;
using UnityEngine;

/// <summary>
/// Managed component for the <see cref="MinimapDisplay"/> unmanaged component.
/// </summary>
class MinimapDisplayAuthoring : MonoBehaviour
{
    /// <summary>
    /// Reference to the entity's minimap icon.
    /// </summary>
    [SerializeField]
    [Tooltip("GameObject used as the minimap icon for this entity.")]
    public GameObject minimapIconGameObject;
}

/// <summary>
/// Baker for the <see cref="MinimapDisplay"/> unmanaged component.
/// </summary>
class MinimapDisplayBaker : Baker<MinimapDisplayAuthoring>
{
    public override void Bake(MinimapDisplayAuthoring authoring)
    {
        Color originalColor = authoring.minimapIconGameObject.GetComponent<SpriteRenderer>().color;
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new MinimapDisplay
        {
            defaultIconColor = originalColor,
            minimapIconEntity = GetEntity(authoring.minimapIconGameObject, TransformUsageFlags.Dynamic),
        });
    }
}

/// <summary>
/// Component data for minimap display.
/// </summary>
public struct MinimapDisplay : IComponentData, IEnableableComponent
{
    /// <summary>
    /// The default color of the minimap icon.
    /// </summary>
    public Color defaultIconColor;

    /// <summary>
    /// Reference to the minimap icon entity.
    /// </summary>
    public Entity minimapIconEntity;
}