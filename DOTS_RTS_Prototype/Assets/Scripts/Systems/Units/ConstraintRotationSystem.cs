using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

partial struct ConstraintRotationSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Initialize target position so that units don't go to (0,0,0) world position by default.
        /* ConstraintRotationJob constraintRotationJob = new ConstraintRotationJob();
        constraintRotationJob.ScheduleParallel(); */
    }
}

public partial struct ConstraintRotationJob : IJobEntity
{
    public void Execute(ref LocalTransform localTransform, in Unit unit)
    {
        float3 euler = math.EulerXYZ(localTransform.Rotation);

        euler.y = 0f;

        localTransform.Rotation = quaternion.EulerXYZ(euler);
    }
}