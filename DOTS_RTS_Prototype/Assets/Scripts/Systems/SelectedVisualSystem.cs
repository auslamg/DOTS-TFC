using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

//TODO: Optimize into Event system and callbacks to avoid RAM calls every tick.
partial struct SelectedVisualSystem : ISystem
{
    /// <summary>
    /// Update() : MonoBehaviour
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Sets gizmo to 0 scale (to make it invisible) on every non-Selected entity
        foreach (RefRO<Selected> selected in SystemAPI.Query<RefRO<Selected>>().WithDisabled<Selected>())
        {
            RefRW<LocalTransform> gizmoLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(selected.ValueRO.selectedGizmoEntity);
            gizmoLocalTransform.ValueRW.Scale = 0;
        }

        // Sets gizmo to full scale (to make it visible) on every Selected entity
        foreach (RefRO<Selected> selected in SystemAPI.Query<RefRO<Selected>>())
        {
            RefRW<LocalTransform> gizmoLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(selected.ValueRO.selectedGizmoEntity);
            gizmoLocalTransform.ValueRW.Scale = selected.ValueRO.displayScale;
        }
    }
}
