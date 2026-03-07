using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

partial struct BarracksSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferences>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();

        foreach ((
            RefRO<LocalTransform> localTransform,
            RefRW<Barracks> barracks,
            DynamicBuffer<SpawnUnitsBuffer> spawnUnitBuffer)
                in SystemAPI.Query<
                RefRO<LocalTransform>,
                RefRW<Barracks>,
                DynamicBuffer<SpawnUnitsBuffer>>())
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

                //FIX: Managed object access nullifies burst compilation
                UnitSO activeUnitSO = GameAssets.Instance.unitRegistrySO.GetUnitSO(barracks.ValueRO.activeUnitKey);

                barracks.ValueRW.maxProgress = activeUnitSO.trainingTime;
            }

            //Progress timer
            barracks.ValueRW.currentProgress += SystemAPI.Time.DeltaTime;
            if (barracks.ValueRO.currentProgress >= barracks.ValueRO.maxProgress)
            {
                barracks.ValueRW.currentProgress = 0;

                UnitKey unitKey = spawnUnitBuffer[0].unitKey;
                //FIX: Managed object access nullifies burst compilation
                UnitSO unitSO = GameAssets.Instance.unitRegistrySO.GetUnitSO(unitKey);

                //Retrieve from buffer to make place for next unit
                spawnUnitBuffer.RemoveAt(0);

                //Spawn unit
                Entity spawnedUnitEntity = state.EntityManager.Instantiate(
                    unitSO.GetPrefabEntity(entitiesReferences));
                SystemAPI.SetComponent<LocalTransform>(spawnedUnitEntity, LocalTransform.FromPosition(
                    localTransform.ValueRO.Position + barracks.ValueRO.spawnPointOffset));

                //Set unit destination to RallyPosition
                RefRW<UnitMover> spawnedUnitMover = SystemAPI.GetComponentRW<UnitMover>(spawnedUnitEntity);
                spawnedUnitMover.ValueRW.targetPosition =
                    localTransform.ValueRO.Position + barracks.ValueRO.rallyPositionOffset;
            }
        }
    }
}
