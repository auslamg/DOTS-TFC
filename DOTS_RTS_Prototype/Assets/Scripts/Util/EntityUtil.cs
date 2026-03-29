using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Utility class for entities and their components.
/// </summary>
[BurstCompile]
public static class EntityUtil
{
    /// <summary>
    /// Validates that an entity exists and is not pending removal.
    /// </summary>
    /// <remarks>
    /// Checks both entity existence and persistence by verifying the presence of a <see cref="LocalTransform"/> component,
    /// which is removed when an entity is queued for destruction. Use in place of plain <see cref="EntityManager.Exists(Entity)"/>,
    /// since that returns <c>true</c> for entities pending removal.
    /// </remarks>
    /// <param name="em">The entity manager used to query existence and component data.</param>
    /// <param name="entity">The entity to validate.</param>
    /// <returns><c>true</c> if the entity exists and has a <see cref="LocalTransform"/> component; otherwise <c>false</c>.</returns>
    public static bool ExistsAndPersists(ref EntityManager em, ref Entity entity)
    {
        if (entity == Entity.Null)
        {
            return false;
        }
        if (!em.Exists(entity))
        {
            return false;
        }
        if (!em.HasComponent<LocalTransform>(entity))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Validates that an entity exists and is not pending removal.
    /// </summary>
    /// <remarks>
    /// Checks both entity existence and persistence by verifying the presence of a <see cref="LocalTransform"/> component,
    /// which is removed when an entity is queued for destruction. Use in place of plain <see cref="EntityManager.Exists(Entity)"/>,
    /// since that returns <c>true</c> for entities pending removal.
    /// </remarks>
    /// <param name="state">The system state providing access to the entity manager.</param>
    /// <param name="entity">The entity to validate.</param>
    /// <returns><c>true</c> if the entity exists and has a <see cref="LocalTransform"/> component; otherwise <c>false</c>.</returns>
    [BurstCompile]
    public static bool ExistsAndPersists(ref SystemState state, in Entity entity)
    {
        EntityManager em = state.EntityManager;
        if (entity == Entity.Null)
        {
            return false;
        }
        if (!em.Exists(entity))
        {
            return false;
        }
        if (!em.HasComponent<LocalTransform>(entity))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Retrieves the <see cref="CollisionWorld"/> from the physics world singleton for use in physics queries.
    /// </summary>
    /// <param name="em">The entity manager used to resolve the <see cref="PhysicsWorldSingleton"/>.</param>
    /// <returns>The <see cref="CollisionWorld"/> from the current <see cref="PhysicsWorldSingleton"/>.</returns>
    public static CollisionWorld GetCollisionWorld(this EntityManager em)
    {
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<PhysicsWorldSingleton>().Build(em);

        PhysicsWorldSingleton physiscsWorldSingleton = query.GetSingleton<PhysicsWorldSingleton>();

        CollisionWorld collisionWorld = physiscsWorldSingleton.CollisionWorld;

        return collisionWorld;
    }
}
