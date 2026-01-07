using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class RandomWalkAuthoring : MonoBehaviour
{
    public float3 targetPostion;
    public float3 originPointPosition;
    public float minDistance;
    public float maxDistance;
    public uint randomSeed;
}

class RandomWalkBaker : Baker<RandomWalkAuthoring>
{
    public override void Bake(RandomWalkAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new RandomWalk
        {
            targetPostion = authoring.targetPostion,
            originPointPosition = authoring.originPointPosition,
            minDistance = authoring.minDistance,
            maxDistance = authoring.maxDistance,
            random = new Unity.Mathematics.Random(
                authoring.randomSeed == 0 ?
                    (uint)entity.Index :
                    authoring.randomSeed)
        });
    }
}

public struct RandomWalk : IComponentData
{
    public float3 targetPostion;
    public float3 originPointPosition;
    public float minDistance;
    public float maxDistance;
    public Unity.Mathematics.Random random;
}
