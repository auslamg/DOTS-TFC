using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;

partial struct BarracksSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitDataRegistry>();
        state.RequireForUpdate<EntityReferencesRegistry>();

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        UnitDataRegistry unitDataRegistry = SystemAPI.GetSingleton<UnitDataRegistry>();
        EntityReferencesRegistry entityReferencesRegistry = SystemAPI.GetSingleton<EntityReferencesRegistry>();

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


            //WIP //TEST

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            EntityReferenceKey testKey = new EntityReferenceKey
            {
                name = "Soldier"
            };
            Debug.Log($"Obtained testKey {testKey}");

            EntityReference testUnitRef = RegistryAccessor.GetEntityReference(ref entityReferencesRegistry.entityReferenceBlobArrayReference, testKey);
            Debug.Log($"Obtained unitRef {testUnitRef}");

            Entity testUnitPrefab = testUnitRef.prefabEntity;
            Debug.Log($"Obtained unitPrefab {testUnitPrefab}");
            
            Debug.Log($"Exists?: {testUnitPrefab != Entity.Null}");

            Debug.Log($"Is prefab?: {state.EntityManager.HasComponent<Prefab>(testUnitPrefab)}");

            Entity testSpawnedUnitEntity = ecb.Instantiate(testUnitPrefab);
            Debug.Log($"Instanced testSpawnedUnitEntity {testSpawnedUnitEntity}");

            //WIP End

            //If the acive unit (already trained) is different from the next one, adjust the progress timer max to start training the next one
            if (barracks.ValueRO.activeUnitKey != spawnUnitBuffer[0].unitKey)
            {
                barracks.ValueRW.activeUnitKey = spawnUnitBuffer[0].unitKey;

                //Retrieve UnitData from UnitKey
                UnitData unitData = RegistryAccessor.GetUnitData(ref unitDataRegistry.unitBlobArrayReference, barracks.ValueRO.activeUnitKey);

                barracks.ValueRW.maxProgress = unitData.trainingTime;
            }

            //Progress timer
            barracks.ValueRW.currentProgress += SystemAPI.Time.DeltaTime;
            if (barracks.ValueRO.currentProgress >= barracks.ValueRO.maxProgress)
            {
                barracks.ValueRW.currentProgress = 0;

                //Retrieve UnitData from UnitKey
                UnitKey unitKey = spawnUnitBuffer[0].unitKey;
                EntityReferenceKey entityKey = new EntityReferenceKey
                {
                    name = unitKey.name
                };

                //Retrieve from buffer to make place for next unit
                spawnUnitBuffer.RemoveAt(0);

                //Spawn unit
                //WIP
                Entity unitPrefab = RegistryAccessor.GetEntityReference(ref entityReferencesRegistry.entityReferenceBlobArrayReference, entityKey).prefabEntity;

                // Only instantiate if valid
                /* if (!state.EntityManager.Exists(unitPrefab))
                {
                    Debug.LogError($"Cannot spawn unit {unitKey.name}: prefab entity is invalid!");
                    continue;
                } */
                Entity spawnedUnitEntity = state.EntityManager.Instantiate(unitPrefab);
                Debug.Log($"Spawn happened with no errors: {spawnedUnitEntity}");

                SystemAPI.SetComponent<LocalTransform>(spawnedUnitEntity, LocalTransform.FromPosition(
                    localTransform.ValueRO.Position + barracks.ValueRO.spawnPointOffset));

                //Set unit destination to RallyPosition
                RefRW<UnitMover> spawnedUnitMover = SystemAPI.GetComponentRW<UnitMover>(spawnedUnitEntity);
                spawnedUnitMover.ValueRW.targetPosition =
                    localTransform.ValueRO.Position + barracks.ValueRO.rallyPositionOffset;
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
