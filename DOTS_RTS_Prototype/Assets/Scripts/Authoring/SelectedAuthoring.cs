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
    public GameObject selectedGizmoGameObject;
    /// <summary>
    /// Display scale for the selected entity's selection gizmo when selected.
    /// </summary>
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
