using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ManualMoveAuthoring : MonoBehaviour
{
}

public class MoveOverrideBaker : Baker<ManualMoveAuthoring>
{
    public override void Bake(ManualMoveAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ManualMove
        {
            targetPosition = authoring.transform.position
        });
        SetComponentEnabled<ManualMove>(entity,false);
    }
}

public struct ManualMove : IComponentData, IEnableableComponent
{
    public float3 targetPosition;
}

