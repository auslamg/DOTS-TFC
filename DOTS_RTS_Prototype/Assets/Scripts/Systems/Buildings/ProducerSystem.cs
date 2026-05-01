using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

partial struct ProducerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<Producer> producer)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<Producer>>())
        {
            if (producer.ValueRW.productionCooldownTimer.Tick(SystemAPI.Time.DeltaTime))
            {
                ResourceManager.Instance?.AddResourceValue(producer.ValueRO.producedResourceKey, producer.ValueRO.productionAmount);
            }
        }
    }
}
