using Unity.Entities;
using UnityEngine;

public class UnitAuthoring : MonoBehaviour
{
    
}

class UnitBaker : Baker<SelectedAuthoring>
{
    public override void Bake(SelectedAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Unit
        {
            
        });
    }
}

public struct Unit : IComponentData
{
    
}