using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="Selected"/> unmanaged component.
/// </summary>
class SelectedAuthoring : MonoBehaviour
{
    /// <summary>
    /// Reference to the selected entity's selection gizmo.
    /// </summary>
    [SerializeField]
    [Tooltip("GameObject used as the selection gizmo for this entity.")]
    public GameObject selectedGizmoGameObject;
    /// <summary>
    /// Display scale for the selected entity's selection gizmo when selected.
    /// </summary>
    [SerializeField]
    [Tooltip("Scale applied to the selection gizmo when this entity is selected.")]
    public float displayScale;
}

/// <summary>
/// Baker for the <see cref="Selected"/> unmanaged component.
/// </summary>
class SelectedBaker : Baker<SelectedAuthoring>
{
    public override void Bake(SelectedAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Selected
        {
            selectedGizmoEntity = GetEntity(authoring.selectedGizmoGameObject, TransformUsageFlags.Dynamic),
            displayScale = authoring.displayScale

        });

        //Default value false
        SetComponentEnabled<Selected>(entity, false);
    }
}


/// <summary>
/// Selection state component used by selection and gizmo display systems.
/// </summary>
public struct Selected : IComponentData, IEnableableComponent
{
    /// <summary>
    /// Reference to the selected entity's selection gizmo.
    /// </summary>
    public Entity selectedGizmoEntity;
    /// <summary>
    /// Display scale for the selected entity's selection gizmo when selected.
    /// </summary>
    public float displayScale;
    /// <summary>
    /// Event-bool triggered on selecting this entity.
    /// </summary>
    /// <remarks>
    /// Custom events are reset at the end of each frame in ResetEventSystem.
    /// </remarks>
    public bool onSelected;
    /// <summary>
    /// Event-bool triggered on deselecting this entity.
    /// </summary>
    /// <remarks>
    /// Custom events are reset at the end of each frame in ResetEventSystem.
    /// </remarks>
    public bool onDeselected;
}
