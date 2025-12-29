using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;


partial struct TestingSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //TestQuery(ref state);        
    }

    /// <summary>
    /// Used for testing queries on Selected enableable component entities.
    /// </summary>
    private void TestQuery(ref SystemState state)
    {
        int unitCount = 0;
        foreach ((
            RefRW<LocalTransform> localTransform,
            RefRO<UnitMover> unitMover,
            RefRW<PhysicsVelocity> physicsVelocity,
            RefRO<Selected> selected)
               in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRO<UnitMover>,
                RefRW<PhysicsVelocity>,
                RefRO<Selected>>())
        {
            unitCount++;
        }
        Debug.Log("unitCount: " + unitCount);
    }
}
