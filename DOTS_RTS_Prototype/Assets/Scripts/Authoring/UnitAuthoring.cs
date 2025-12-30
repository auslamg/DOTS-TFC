using Unity.Entities;
using UnityEngine;

//TODO: Implement owners with ID's as Data
public class UnitAuthoring : MonoBehaviour
{
    public Faction faction;
    public int ownerID;
}

class UnitBaker : Baker<UnitAuthoring>
{
    public override void Bake(UnitAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Unit
        {
            faction = authoring.faction,
            ownerID = authoring.ownerID
        });
    }
}

public struct Unit : IComponentData
{
    public Faction faction;
    public int ownerID;

}