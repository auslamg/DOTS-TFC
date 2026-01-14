using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <c>MeleeAttack</c> unmanaged component.
/// </summary>
class MeleeAttackAuthoring : MonoBehaviour
{
    /// <summary>
    /// Time span between attacks.
    /// </summary>
    public float attackFrequency;
    /// <summary>
    /// Maximum distance for attacks.
    /// </summary>
    //TODO: Rename to attackRange
    public float attackDistance;
    /// <summary>
    /// Base damage dealt with each attack.
    /// </summary>
    public int damageAmount;
}

/// <summary>
/// Baker for the <c>MeleeAttack</c> unmanaged component.
/// </summary>
class MeleeAttackBaker : Baker<MeleeAttackAuthoring>
{
    public override void Bake(MeleeAttackAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new MeleeAttack
        {
            attackFrequency = authoring.attackFrequency,
            attackDistance = authoring.attackDistance,
            damageAmount = authoring.damageAmount,
        });
    }
}

public struct MeleeAttack : IComponentData
{
    /// <summary>
    /// Current time passed since the last attack.
    /// </summary>
    public float attackPhaseTime;
    /// <summary>
    /// Time span between attacks.
    /// </summary>
    public float attackFrequency;
    /// <summary>
    /// Maximum distance for attacks.
    /// </summary>
    //TODO: Rename to attackRange

    public float attackDistance;
    /// <summary>
    /// Base damage dealt with each attack.
    /// </summary>
    public int damageAmount;
    /// <summary>
    /// Event-bool triggered on melee attack.
    /// </summary>
    /// <remarks>
    /// Custom events are reset at the end of each frame in ResetEventSystem.
    /// </remarks>
    public bool onAttack;
}
