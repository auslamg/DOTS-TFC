using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="TestStructs"/> unmanaged component.
/// </summary>
class TestStructsAuthoring : MonoBehaviour
{
    /// <summary>
    /// Time interval between ticks for the testing timer.
    /// </summary>
    [SerializeField]
    [Tooltip("Time interval between ticks for the testing timer.")]
    public float tickTimerInterval;
}

/// <summary>
/// Baker for the <see cref="TestStructs"/> unmanaged component.
/// </summary>
class TestStructsBaker : Baker<TestStructsAuthoring>
{
    public override void Bake(TestStructsAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new TestStructs
        {
            tickTimer = new LoopingTimer
            {
                Time = authoring.tickTimerInterval,
                Interval = authoring.tickTimerInterval
            }
        });
    }
}

/// <summary>
/// Used to test custom utility structs.
/// </summary>
public struct TestStructs : IComponentData
{
    /// <summary>
    /// Testing field for <see cref="LoopingTimer"/>.
    /// </summary>
    public LoopingTimer tickTimer;
}