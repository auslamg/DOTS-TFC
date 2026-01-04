using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class UnitMoverAuthoring : MonoBehaviour
{
    public float moveSpeed;
    public float rotationSpeed;
    public float targetReachedDistanceSquared = 2f;
}

public class UnitMoverBaker : Baker<UnitMoverAuthoring>
{
    public override void Bake(UnitMoverAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new UnitMover
        {
            moveSpeed = authoring.moveSpeed,
            rotationSpeed = authoring.rotationSpeed,
            targetReachedDistanceSquared = authoring.targetReachedDistanceSquared,
            targetPosition = authoring.transform.position
        });
    }
}

public struct UnitMover : IComponentData
{
    public float moveSpeed;
    public float rotationSpeed;
    public float targetReachedDistanceSquared;

    public float3 targetPosition;
}

