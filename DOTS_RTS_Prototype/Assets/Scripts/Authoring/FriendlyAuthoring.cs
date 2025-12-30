using Unity.Entities;
using UnityEngine;

class FriendlyAuthoring : MonoBehaviour
{
    
}

class FriendlyBaker : Baker<FriendlyAuthoring>
{
    public override void Bake(FriendlyAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Friendly
        {
            
        });
    }
}

public struct Friendly : IComponentData
{

}
