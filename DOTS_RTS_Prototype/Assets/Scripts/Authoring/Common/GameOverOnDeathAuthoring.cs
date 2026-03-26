using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// Managed component for the <see cref="GameOverOnDeath"/> unmanaged component.
/// </summary>
public class GameOverOnDeathAuthoring : MonoBehaviour
{
    public string gameOverMessage;
}

/// <summary>
/// Baker for the <see cref="GameOverOnDeath"/> unmanaged component.
/// </summary>
class GameOverOnDeathBaker : Baker<GameOverOnDeathAuthoring>
{
    public override void Bake(GameOverOnDeathAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new GameOverOnDeath
        {
            gameOverMessage = authoring.gameOverMessage,
        });
    }
}

/// <summary>
/// Used by the fort building.
/// </summary>
public struct GameOverOnDeath : IComponentData, IEnableableComponent
{
    public FixedString64Bytes gameOverMessage;
}
