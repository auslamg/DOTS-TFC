using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateBefore(typeof(ResetEventSystem))]
partial struct SelectedVisualSystem : ISystem
{
    /// <summary>
    /// Update() : MonoBehaviour
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (RefRO<Selected> selected in SystemAPI.Query<RefRO<Selected>>().WithPresent<Selected>())
        {
            //NOTE:
            //onDeselected must execute first to avoid a visual bug when selecting already-selected units.
            
            if (selected.ValueRO.onDeselected)
            {
                // Sets gizmo to 0 scale to make it invisible
                RefRW<LocalTransform> gizmoLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(selected.ValueRO.selectedGizmoEntity);
                gizmoLocalTransform.ValueRW.Scale = 0;
            }
            if (selected.ValueRO.onSelected)
            {
                // Sets gizmo to default scale to make it visible
                RefRW<LocalTransform> gizmoLocalTransform = SystemAPI.GetComponentRW<LocalTransform>(selected.ValueRO.selectedGizmoEntity);
                gizmoLocalTransform.ValueRW.Scale = selected.ValueRO.displayScale;
            }
        }
    }
}
