using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="MeleeAttack"/> unmanaged component.
/// </summary>
class MeleeAttackAuthoring : MonoBehaviour
{
    /// <summary>
    /// Time interval between attacks.
    /// </summary>
    [SerializeField]
    [Tooltip("Time interval between melee attacks.")]
    public float attackFrequency;
    /// <summary>
    /// Maximum attack range.
    /// </summary>
    //TODO: Rename to attackRange
    [SerializeField]
    [Tooltip("Maximum distance at which melee attacks can be performed.")]
    public float attackDistance;
    /// <summary>
    /// Base damage dealt with each attack.
    /// </summary>
    [SerializeField]
    [Tooltip("Base damage dealt by each melee attack.")]
    public int damageAmount;
}

/// <summary>
/// Baker for the <see cref="MeleeAttack"/> unmanaged component.
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

/// <summary>
/// Used by entities that can perform a melee attack.
/// </summary>
/// <remarks>
/// Requires the <see cref="Targetter"/> component 
/// //IDEA: Enforce implementation through [RequireComponent(typeof(Targetter))]
/// </remarks>
public struct MeleeAttack : IComponentData
{
    /// <summary>
    /// Current time passed since the last attack.
    /// </summary>
    public float attackPhaseTime;
    /// <summary>
    /// Time interval between attacks.
    /// </summary>
    public float attackFrequency;
    /// <summary>
    /// Maximum attack range.
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
