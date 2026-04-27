using Unity.Entities;
using UnityEngine;

class MinimapDisplayAuthoring : MonoBehaviour
{
    /// <summary>
    /// Reference to the entity's minimap icon.
    /// </summary>
    [SerializeField]
    [Tooltip("GameObject used as the minimap icon for this entity.")]
    public GameObject minimapIconGameObject;
}

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
/// Selection state component used by selection and gizmo display systems.
/// </summary>
public struct MinimapDisplay : IComponentData, IEnableableComponent
{
    /// <summary>
    /// 
    /// </summary>
    public Color defaultIconColor;

    /// <summary>
    /// Reference to the selected entity's selection gizmo.
    /// </summary>
    public Entity minimapIconEntity;
}