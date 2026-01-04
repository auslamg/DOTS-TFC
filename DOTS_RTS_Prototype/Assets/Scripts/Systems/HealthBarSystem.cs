using System.Numerics;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
partial struct HealthBarSystem : ISystem
{
    //Disabled due to camera access
    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //Note:
        //Access to managed Camera.main is non-compatible with burst-compiler
        UnityEngine.Vector3 cameraForward = Camera.main != null ? Camera.main.transform.forward : UnityEngine.Vector3.zero;

        foreach ((
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

            //[Deprecated]
            //Alternative for TransformFlags.Dynamic (no scale PostTransformMatix operations but uniform scale)
            /* RefRW<LocalTransform> visualBarLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(healthbar.ValueRO.visualBarEntity);
            visualBarLocalTransform.ValueRW.Scale = healthNormalized; */

        }
    }
}
