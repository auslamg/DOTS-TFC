using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct HealthBarSystem : ISystem
{
    [ReadOnly] public ComponentLookup<LocalTransform> localTransformComponentLookup;
    [ReadOnly] public ComponentLookup<Health> healthComponentLookup;
    [ReadOnly] public ComponentLookup<PostTransformMatrix> postTransformMatrixComponentLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        localTransformComponentLookup = state.GetComponentLookup<LocalTransform>();
        healthComponentLookup = state.GetComponentLookup<Health>();
        postTransformMatrixComponentLookup = state.GetComponentLookup<PostTransformMatrix>();
    }

    //[BurstCompile] //Disabled due to camera access
    public void OnUpdate(ref SystemState state)
    {
        localTransformComponentLookup.Update(ref state);
        healthComponentLookup.Update(ref state);
        postTransformMatrixComponentLookup.Update(ref state);

        //Note:
        //Access to managed Camera.main is non-compatible with burst-compiler
        UnityEngine.Vector3 cameraForward = Camera.main != null ? Camera.main.transform.forward : UnityEngine.Vector3.zero;

        new HealthBarJob
        {
            cameraForward = cameraForward,
            localTransformComponentLookup = localTransformComponentLookup,
            healthComponentLookup = healthComponentLookup,
            postTransformMatrixComponentLookup = postTransformMatrixComponentLookup,
        }.ScheduleParallel();

        /* foreach ((
            RefRW<LocalTransform> localTransform,
            RefRO<HealthBar> healthBar)
                in SystemAPI.Query<
                RefRW<LocalTransform>,
                RefRO<HealthBar>>())
        {

            //Turn healthbar towards camera converting global camera direction into local
            LocalTransform parentLocalTransform = SystemAPI.GetComponent<LocalTransform>(healthBar.ValueRO.healthEntity);
            //TODO: Extract or implement additional conditions
            if (localTransform.ValueRO.Scale == 1f) //AND unit is not selected AND setting is enabled
            {
                //Healthbar is visible
                //TODO: Take this out if the entire conditional check doesn't happen here
                localTransform.ValueRW.Rotation = parentLocalTransform.InverseTransformRotation(quaternion.LookRotation(cameraForward, math.up()));
            }

            Health health = SystemAPI.GetComponent<Health>(healthBar.ValueRO.healthEntity);

            //FIX: Avoid continue. Maybe labels/goto?
            if (!health.onHealthChanged)
            {
                continue;
            }
            float healthNormalized = (float)health.currentHealth / health.maxHealth;

            //Make bar invisible if at full health
            localTransform.ValueRW.Scale = healthNormalized == 1 ? 0 : 1;

            RefRW<PostTransformMatrix> visualBarPostTransformMatrix = SystemAPI.GetComponentRW<PostTransformMatrix>(healthBar.ValueRO.visualBarEntity);
            visualBarPostTransformMatrix.ValueRW.Value = float4x4.Scale(healthNormalized, 1, 1);
        } */
    }
}

[BurstCompile]
public partial struct HealthBarJob : IJobEntity
{
    [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> localTransformComponentLookup;
    [ReadOnly] public ComponentLookup<Health> healthComponentLookup;
    [NativeDisableParallelForRestriction] public ComponentLookup<PostTransformMatrix> postTransformMatrixComponentLookup;
    public float3 cameraForward;
    public void Execute(in HealthBar healthBar, Entity entity)
    {
        RefRW<LocalTransform> localTransform = localTransformComponentLookup.GetRefRW(entity);
        //Turn healthbar towards camera converting global camera direction into local
        LocalTransform parentLocalTransform = localTransformComponentLookup[healthBar.healthEntity];
        //TODO: Extract or implement additional conditions //AND unit is not selected AND setting is enabled
        if (localTransform.ValueRO.Scale == 1f)
        {
            //Healthbar is visible
            //TODO: Take this out if the entire conditional check doesn't happen here
            localTransform.ValueRW.Rotation = parentLocalTransform.InverseTransformRotation(quaternion.LookRotation(cameraForward, math.up()));
        }

        Health health = healthComponentLookup[healthBar.healthEntity];

        if (health.onHealthChanged)
        {
            float healthNormalized = (float)health.currentHealth / health.maxHealth;

            //TODO: Extract or implement additional conditions //AND unit is not selected AND setting is enabled
            //Make bar invisible if at full health
            localTransform.ValueRW.Scale = healthNormalized == 1 ? 0 : 1;

            RefRW<PostTransformMatrix> visualBarPostTransformMatrix = postTransformMatrixComponentLookup.GetRefRW(healthBar.visualBarEntity);
            visualBarPostTransformMatrix.ValueRW.Value = float4x4.Scale(healthNormalized, 1, 1);

            //[Deprecated]
            //Alternative for TransformFlags.Dynamic (no scale PostTransformMatrix operations but uniform scale)
            /* RefRW<LocalTransform> visualBarLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(healthbar.ValueRO.visualBarEntity);
            visualBarLocalTransform.ValueRW.Scale = healthNormalized; */
        }

    }

}