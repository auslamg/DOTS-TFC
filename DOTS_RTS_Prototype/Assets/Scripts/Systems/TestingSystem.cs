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
        //TestQuerySelected(ref state); 
        TestQueryFriendly(ref state);

    }

    /// <summary>
    /// Used for testing queries on Selected component entities.
    /// </summary>
    private void TestQuerySelected(ref SystemState state)
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

    /// <summary>
    /// Used for testing queries on Friendly component entities.
    /// </summary>
    private void TestQueryFriendly(ref SystemState state)
    {
        int unitCount = 0;
        foreach (
            RefRW<Friendly> friendly
                in SystemAPI.Query<
                RefRW<Friendly>>())
        {
            unitCount++;
        }
        Debug.Log("unitCount: " + unitCount);
    }
}
