using Unity.Entities;
using UnityEngine;

class EnemySpawnerAuthoring : MonoBehaviour
{
    public float spawnFrequency;
    public float minDistance;
    public float maxDistance;
}

class EnemySpawnerAuthoringBaker : Baker<EnemySpawnerAuthoring>
{
    public override void Bake(EnemySpawnerAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemySpawner
        {
            spawnFrequency = authoring.spawnFrequency,
            minDistance = authoring.minDistance,
            maxDistance = authoring.maxDistance,
        });
    }
}

public struct EnemySpawner : IComponentData {
    public float spawnPhaseTime;
    public float spawnFrequency;
    public float minDistance;
    public float maxDistance;

}
