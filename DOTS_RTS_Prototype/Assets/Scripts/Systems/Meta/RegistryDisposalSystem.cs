using Unity.Burst;
using Unity.Entities;

/// <summary>
/// Handles disposal of singleton registries when the system is destroyed.
/// </summary>
partial struct RegistryDisposalSystem : ISystem
{
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        /* if (SystemAPI.HasSingleton<AnimationDataRegistry>())
        {
            RefRW<AnimationDataRegistry> animationDataRegistry = SystemAPI.GetSingletonRW<AnimationDataRegistry>();
            if (animationDataRegistry.ValueRW.animationDataBlobArrayReference.IsCreated)
            {
                animationDataRegistry.ValueRW.animationDataBlobArrayReference.Dispose();
            }
        } */

        /* if (SystemAPI.HasSingleton<BuildingDataRegistry>())
        {
            RefRW<BuildingDataRegistry> buildingDataRegistry = SystemAPI.GetSingletonRW<BuildingDataRegistry>();
            if (buildingDataRegistry.ValueRW.buildingBlobArrayReference.IsCreated)
            {
                buildingDataRegistry.ValueRW.buildingBlobArrayReference.Dispose();
            }
        } */

        /* if (SystemAPI.HasSingleton<UnitDataRegistry>())
        {
            RefRW<UnitDataRegistry> unitDataRegistry = SystemAPI.GetSingletonRW<UnitDataRegistry>();
            if (unitDataRegistry.ValueRW.unitBlobArrayReference.IsCreated)
            {
                unitDataRegistry.ValueRW.unitBlobArrayReference.Dispose();
            }
        } */
    }
}
