using Unity.Burst;
using Unity.Entities;


[UpdateInGroup(typeof(LateSimulationSystemGroup), OrderFirst = true)]
/// <summary>
/// Dispatches game-over and critical-entity events based on tracked faction deaths.
/// </summary>
partial struct GameOverOnDeathSystem : ISystem
{
    /// <summary>
    /// Monitors critical entities and raises managed events for game-over and critical tracking.
    /// </summary>
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // If single criticals are killed, game over
        foreach ((
            RefRO<Health> health,
            RefRO<GameOverOnDeath> gameOverOnDeath,
            RefRO<Faction> faction,
            Entity entity)
                in SystemAPI.Query<
                RefRO<Health>,
                RefRO<GameOverOnDeath>,
                RefRO<Faction>>().
                WithEntityAccess())
        {
            if (faction.ValueRO.factionID.Equals(GameAssets.PLAYER_FACTION))
            {
                if (health.ValueRO.onDeath)
                {
                    string msg = gameOverOnDeath.ValueRO.gameOverMessage.ToString();
                    DOTSEventManager.Instance.TriggerOnGameOver(msg);
                }
            }
        }

        // If group criticals are built, register them
        foreach ((
            RefRO<GameOverOnGroupDeath> gameOverOnGroupDeath,
            RefRO<Faction> faction,
            Entity entity)
                in SystemAPI.Query<
                RefRO<GameOverOnGroupDeath>,
                RefRO<Faction>>().
                WithEntityAccess())
        {
            if (faction.ValueRO.factionID.Equals(GameAssets.PLAYER_FACTION))
            {
                if (!gameOverOnGroupDeath.ValueRO.registered)
                {
                    DOTSEventManager.Instance.TriggerOnCriticalConstruction(entity);
                }
            }
        }

        // If group criticals are killed, remove them
        foreach ((
            RefRO<Health> health,
            RefRO<GameOverOnGroupDeath> gameOverOnGroupDeath,
            RefRO<Faction> faction,
            Entity entity)
                in SystemAPI.Query<
                RefRO<Health>,
                RefRO<GameOverOnGroupDeath>,
                RefRO<Faction>>().
                WithEntityAccess())
        {
            if (faction.ValueRO.factionID.Equals(GameAssets.PLAYER_FACTION))
            {
                if (health.ValueRO.onDeath)
                {
                    DOTSEventManager.Instance.TriggerOnCriticalDestruction(entity);
                }
            }
        }
    }
}
