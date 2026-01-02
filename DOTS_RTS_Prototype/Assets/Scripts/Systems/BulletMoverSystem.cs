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
                  RefRO<Targetter> targetter,
                  Entity entity)
                    in SystemAPI.Query<
                    RefRW<LocalTransform>,
                    RefRO<Bullet>,
                    RefRO<Targetter>>().
                    WithEntityAccess())
        {
            //IDEA: Extract into EntityUtil.Exists() method
            //FIX: Avoid continue. Maybe labels/goto?
            //If there is no target, destroy this and go for next entity
            if (targetter.ValueRO.targetEntity == Entity.Null || !SystemAPI.HasComponent<LocalTransform>(targetter.ValueRO.targetEntity))
            {
                entityCommandBuffer.DestroyEntity(entity);
                continue;
            }

            LocalTransform targetLocalTransform = SystemAPI.GetComponent<LocalTransform>(targetter.ValueRO.targetEntity);
            Shootable targetShootable = SystemAPI.GetComponent<Shootable>(targetter.ValueRO.targetEntity);
            float3 targetPosition = targetLocalTransform.TransformPoint(targetShootable.hitPointPosition);

            float distanceBeforeSquared = math.distancesq(localTransform.ValueRO.Position, targetPosition);

            //Caclculate move direction
            float3 moveDirection = targetPosition - localTransform.ValueRO.Position;
            moveDirection = math.normalize(moveDirection);

            //Move towards target
            localTransform.ValueRW.Position += moveDirection * bullet.ValueRO.speed * SystemAPI.Time.DeltaTime;

            float distanceAfterSquared = math.distancesq(localTransform.ValueRO.Position, targetPosition);

            //Overshoot clipping countermeasure
            if (distanceBeforeSquared < distanceAfterSquared)
            {
                //Overshot
                localTransform.ValueRW.Position = targetPosition;
            }

            //Destroy bullet and apply effects when close enough to target
            float destroyDistanceSquared = .2f;
            if (math.distancesq(localTransform.ValueRO.Position, targetPosition) <= destroyDistanceSquared)
            {
                //Close enough to damage target
                RefRW<Health> targetHealth = SystemAPI.GetComponentRW<Health>(targetter.ValueRO.targetEntity);
                targetHealth.ValueRW.currentHealth -= bullet.ValueRO.damageAmount;
                targetHealth.ValueRW.onHealthChanged = true;

                entityCommandBuffer.DestroyEntity(entity);
            }
        }
    }
}
