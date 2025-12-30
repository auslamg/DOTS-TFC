using Unity.Entities;
using UnityEngine;

class SelectedAuthoring : MonoBehaviour
{
    public GameObject selectedGizmoGameObject;
    public float displayScale;
}

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
    public Entity selectedGizmoEntity;
    public float displayScale;

    public bool onSelected;
    public bool onDeselected;
}
