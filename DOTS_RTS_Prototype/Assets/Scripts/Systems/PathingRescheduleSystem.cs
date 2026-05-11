using Unity.Burst;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Reschedules pathfinding requests when the grid's pathing map is updated due to new obstacles.
/// </summary>
[UpdateBefore(typeof(GridSystem))]
partial struct PathingRescheduleSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
           RefRW<PathingReschedule> pathingReschedule,
           RefRO<FlowFieldFollower> flowFieldFollower,
           Entity entity)
               in SystemAPI.Query<
               RefRW<PathingReschedule>,
               RefRO<FlowFieldFollower>>().
               WithEntityAccess())
        {
            if (pathingReschedule.ValueRW.attemptTimer.Tick(Time.deltaTime))
            {
                GridData gridData = SystemAPI.GetSingleton<GridData>();

                int flowFieldIndex = flowFieldFollower.ValueRO.flowFieldIndex;
                uint flowFieldVer = gridData.flowFieldArray[flowFieldIndex].pathingMapVersion;
                /* Debug.Log($"[Reschedule] Trying reschedule!{flowFieldVer} - {gridData.pathingMapVersion}"); */

                if (flowFieldVer != gridData.pathingMapVersion)
                {
                    /* Debug.Log("[Reschedule] Rescheduling Path Request!") */;

                    PathRequest pathRequest = SystemAPI.GetComponent<PathRequest>(entity);

                    pathRequest.targetPosition = flowFieldFollower.ValueRO.targetPosition;
                    pathRequest.postFormationPosition = flowFieldFollower.ValueRO.postFormationPosition;

                    SystemAPI.SetComponent(entity,pathRequest);
                    SystemAPI.SetComponentEnabled<PathRequest>(entity, true);
                }
            }
            
        }
    }
}
