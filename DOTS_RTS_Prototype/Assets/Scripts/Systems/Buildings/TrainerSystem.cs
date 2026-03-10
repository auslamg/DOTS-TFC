using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

partial struct TrainerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitDataRegistry>();
        state.RequireForUpdate<EntityPrefabsRegistry>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        UnitDataRegistry unitDataRegistry = SystemAPI.GetSingleton<UnitDataRegistry>();
        Entity entityReferencesRegistryEntity = SystemAPI.GetSingletonEntity<EntityPrefabsRegistry>();
        DynamicBuffer<EntityReference> entityReferencesBuffer = SystemAPI.GetBuffer<EntityReference>(entityReferencesRegistryEntity);

        EntityCommandBuffer ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);


        foreach ((
            RefRW<Trainer> trainer,
            RefRO<TrainUnitRequest> queuedUnit,
            DynamicBuffer<QueuedUnitBuffer> spawnUnitBuffer,
            EnabledRefRW<TrainUnitRequest> queuedUnitEnabled)
                in SystemAPI.Query<
                RefRW<Trainer>,
                RefRO<TrainUnitRequest>,
                DynamicBuffer<QueuedUnitBuffer>,
                EnabledRefRW<TrainUnitRequest>>())
        {
            spawnUnitBuffer.Add(new QueuedUnitBuffer
            {
                unitKey = queuedUnit.ValueRO.unitKey
            });
            queuedUnitEnabled.ValueRW = false;

            trainer.ValueRW.onUnitQueueChange = true;
        }

        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<Trainer> trainer,
            DynamicBuffer<QueuedUnitBuffer> spawnUnitBuffer,
            Entity entity)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<Trainer>,
                DynamicBuffer<QueuedUnitBuffer>>().
                WithEntityAccess())
        {
            //No units in queue
            if (spawnUnitBuffer.IsEmpty)
            {
                continue;
            }


            //If the acive unit (already trained) is different from the next one, adjust the progress timer max to start training the next one
            if (trainer.ValueRO.activeUnitKey != spawnUnitBuffer[0].unitKey)
            {
                trainer.ValueRW.activeUnitKey = spawnUnitBuffer[0].unitKey;

                //Retrieve UnitData from UnitKey
                UnitData unitData = RegistryAccessor.GetUnitData(ref unitDataRegistry.unitBlobArrayReference, trainer.ValueRO.activeUnitKey);

                trainer.ValueRW.maxProgress = unitData.trainingTime;
            }

            // Progress timer
            trainer.ValueRW.currentProgress += SystemAPI.Time.DeltaTime;
            if (trainer.ValueRO.currentProgress >= trainer.ValueRO.maxProgress)
            {
                trainer.ValueRW.currentProgress = 0;

                // Retrieve UnitData from UnitKey
                UnitKey unitKey = spawnUnitBuffer[0].unitKey;
                EntityReferenceKey entityKey = new EntityReferenceKey
                {
                    name = unitKey.name
                };

                // Retrieve from buffer to make place for next unit
                spawnUnitBuffer.RemoveAt(0);
                trainer.ValueRW.onUnitQueueChange = true;

                // Spawn unit
                int unitRefIndex = RegistryAccessor.GetEntityReferenceIndex(ref entityReferencesBuffer, entityKey);
                if (unitRefIndex < 0)
                {
                    Debug.LogError($"Cannot spawn unit '{unitKey.name}': no entity reference key found in registry.");
                    continue;
                }

                Entity unitPrefab = entityReferencesBuffer[unitRefIndex].prefabEntity;

                if (!state.EntityManager.Exists(unitPrefab) ||
                    !state.EntityManager.HasComponent<Prefab>(unitPrefab))
                {
                    Debug.LogError($"Cannot spawn unit '{unitKey.name}': referenced entity is not a valid prefab. Check EntitiesReferencesAuthoring assignments.");
                    continue;
                }

                // Instantiate the unit prefab
                Entity spawnedUnitEntity = ecb.Instantiate(unitPrefab);

                // Set unit position to spawn point
                LocalTransform spawnedTransform = state.EntityManager.GetComponentData<LocalTransform>(unitPrefab);
                spawnedTransform.Position = localTransform.ValueRO.Position + trainer.ValueRO.spawnPointOffset;
                ecb.SetComponent(spawnedUnitEntity, spawnedTransform);

                // Set unit destination to RallyPosition.
                UnitMover unitMover = state.EntityManager.GetComponentData<UnitMover>(unitPrefab);
                unitMover.targetPosition = localTransform.ValueRO.Position + trainer.ValueRO.rallyPositionOffset;
                ecb.SetComponent(spawnedUnitEntity, unitMover);
            }
        }
    }
}
