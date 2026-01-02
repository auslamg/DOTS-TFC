using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
partial struct BulletMoverSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer entityCommandBuffer =
            SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((RefRW<LocalTransform> localTransform,
                  RefRO<Bullet> bullet,
                  RefRO<Target> target,
                  Entity entity)
                    in SystemAPI.Query<
                    RefRW<LocalTransform>,
                    RefRO<Bullet>,
                    RefRO<Target>>().
                    WithEntityAccess())
        {
            if (target.ValueRO.targetEntity == Entity.Null)
            {
                entityCommandBuffer.DestroyEntity(entity);
            }

            LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(target.ValueRO.targetEntity);

            float distanceBeforeSquared = math.distancesq(localTransform.ValueRO.Position, targetLocalTransform.Position);

            //Caclculate move direction
            float3 moveDirection = targetLocalTransform.Position - localTransform.ValueRO.Position;
            moveDirection = math.normalize(moveDirection);

            //Move towards target
            localTransform.ValueRW.Position += moveDirection * bullet.ValueRO.speed * SystemAPI.Time.DeltaTime;

            float distanceAfterSquared = math.distancesq(localTransform.ValueRO.Position, targetLocalTransform.Position);

            //Overshoot clipping countermeasure
            if (distanceBeforeSquared < distanceAfterSquared)
            {
                //Overshot
                localTransform.ValueRW.Position = targetLocalTransform.Position;
            }

            //Destroy bullet and apply effects when close enough to target
            float destroyDistanceSquared = .2f;
            if (math.distancesq(localTransform.ValueRO.Position, targetLocalTransform.Position) <= destroyDistanceSquared)
            {
                //Close enough to damage target
                RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(target.ValueRO.targetEntity);
                targetHealth.ValueRW.currentHealth -= bullet.ValueRO.damageAmount;

                entityCommandBuffer.DestroyEntity(entity);
            }
        }
    }
}
