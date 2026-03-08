using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

partial struct BarracksSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitDataRegistry>();
        state.RequireForUpdate<EntitiesReferences>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        UnitDataRegistry unitDataRegistry = SystemAPI.GetSingleton<UnitDataRegistry>();
        Entity entityReferencesRegistryEntity = SystemAPI.GetSingletonEntity<EntitiesReferences>();
        DynamicBuffer<EntityReference> entityReferencesBuffer = SystemAPI.GetBuffer<EntityReference>(entityReferencesRegistryEntity);
        EntityCommandBuffer ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged);

        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<Barracks> barracks,
            DynamicBuffer<SpawnUnitsBuffer> spawnUnitBuffer,
            Entity entity)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<Barracks>,
                DynamicBuffer<SpawnUnitsBuffer>>().
                WithEntityAccess())
        {
            //No units in queue
            if (spawnUnitBuffer.IsEmpty)
            {
                continue;
            }


            //If the acive unit (already trained) is different from the next one, adjust the progress timer max to start training the next one
            if (barracks.ValueRO.activeUnitKey != spawnUnitBuffer[0].unitKey)
            {
                barracks.ValueRW.activeUnitKey = spawnUnitBuffer[0].unitKey;

                //Retrieve UnitData from UnitKey
                UnitData unitData = RegistryAccessor.GetUnitData(ref unitDataRegistry.unitBlobArrayReference, barracks.ValueRO.activeUnitKey);

                barracks.ValueRW.maxProgress = unitData.trainingTime;
            }

            // Progress timer
            barracks.ValueRW.currentProgress += SystemAPI.Time.DeltaTime;
            if (barracks.ValueRO.currentProgress >= barracks.ValueRO.maxProgress)
            {
                barracks.ValueRW.currentProgress = 0;

                // Retrieve UnitData from UnitKey
                UnitKey unitKey = spawnUnitBuffer[0].unitKey;
                EntityReferenceKey entityKey = new EntityReferenceKey
                {
                    name = unitKey.name
                };

                // Retrieve from buffer to make place for next unit
                spawnUnitBuffer.RemoveAt(0);

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
                spawnedTransform.Position = localTransform.ValueRO.Position + barracks.ValueRO.spawnPointOffset;
                ecb.SetComponent(spawnedUnitEntity, spawnedTransform);

                // Set unit destination to RallyPosition.
                UnitMover unitMover = state.EntityManager.GetComponentData<UnitMover>(unitPrefab);
                unitMover.targetPosition = localTransform.ValueRO.Position + barracks.ValueRO.rallyPositionOffset;
                ecb.SetComponent(spawnedUnitEntity, unitMover);
            }
        }
    }
}
