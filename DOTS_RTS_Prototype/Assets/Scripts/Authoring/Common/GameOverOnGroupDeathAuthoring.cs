using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="GameOverOnGroupDeath"/> unmanaged component.
/// </summary>
public class GameOverOnGroupDeathAuthoring : MonoBehaviour
{
    /// <summary>
    /// Critical group tags this entity contributes to for group-based loss conditions.
    /// </summary>
    [SerializeField]
    [Tooltip("Critical group tags used for group-based game-over conditions.")]
    public List<CriticalEntityGroupSO> groupTags = new List<CriticalEntityGroupSO>();
}

/// <summary>
/// Baker for the <see cref="GameOverOnGroupDeath"/> unmanaged component.
/// </summary>
class GameOverOnGroupDeathBaker : Baker<GameOverOnGroupDeathAuthoring>
{
    public override void Bake(GameOverOnGroupDeathAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        if (authoring.groupTags != null &&
            authoring.groupTags.Count > 0)
        {
            AddComponent(entity, new GameOverOnGroupDeath
            {
                registered = false
            });

            DynamicBuffer<GameOverOnGroupDeathTag> tagsBuffer = AddBuffer<GameOverOnGroupDeathTag>(entity);
            foreach (CriticalEntityGroupSO group in authoring.groupTags)
            {
                if (group == null || string.IsNullOrWhiteSpace(group.groupName))
                {
                    continue;
                }

                tagsBuffer.Add(new GameOverOnGroupDeathTag
                {
                    value = new FixedString64Bytes(group.groupName)
                });
            }
        }
        else
        {
            Debug.LogWarning($"A critical entity with no groupTag was submitted: GO[{authoring.gameObject.name}] Entity [{entity}]");
        }
    }
}

/// <summary>
/// Used by the fort building.
/// </summary>
public struct GameOverOnGroupDeath : IComponentData, IEnableableComponent
{
    /// <summary>
    /// Tracks whether this entity has already been registered in runtime loss-condition state.
    /// </summary>
    public bool registered;
}

/// <summary>
/// Buffer element storing one critical group tag used by group-death game over logic.
/// </summary>
public struct GameOverOnGroupDeathTag : IBufferElementData
{
    /// <summary>
    /// Group name value.
    /// </summary>
    public FixedString64Bytes value;
}
