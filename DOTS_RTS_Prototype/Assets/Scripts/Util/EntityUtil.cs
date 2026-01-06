using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

/// <summary>
/// Utility class for entities and their components.
/// </summary>
/// <remarks>
/// The component is a Singleton.
/// </remarks>
public static class EntityUtil
{

    /// <summary>
    /// Validates a that an Entity exists.
    /// </summary>
    /// <remarks>
    /// The method validates an entity checking if it actually exists and if it's queued for removal (by checking if it contains a LocalTransform component). If any of both conditions fail, returns false.
    /// This must be used in place of plain <see cref="EntityManager.Exists(Entity)"/>, since entities queued for removal returns true in said method.
    /// </remarks>
    public static bool ExistsAndPersists(this EntityManager em, Entity entity)
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
    /// Validates a that an Entity exists.
    /// </summary>
    /// <remarks>
    /// The method validates an entity checking if it actually exists and if it's queued for removal (by checking if it contains a LocalTransform component). If any of both conditions fail, returns false.
    /// This must be used in place of default <see cref="EntityManager.ExistsAndPersists(Entity entity)"/> in Jobs and parallel code, since EntityManagers are main-thread only and must remain thread-safe. Conditions are checked through lookups on this override.
    /// </remarks>
    public static bool ExistsAndPersists(this Entity entity, EntityStorageInfoLookup esiLookup, ComponentLookup<LocalTransform> componentLookup)
    {
        if (!esiLookup.Exists(entity))
        {
            return false;
        }
        if (!componentLookup.HasComponent(entity))
        {
            return false;
        }
        return true;
    }

    public static void GetEntityLookups(ref SystemState state, out ComponentLookup<LocalTransform> localTransformLookup, out EntityStorageInfoLookup entityStorageInfoLookup)
    {
        localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
        entityStorageInfoLookup = state.GetEntityStorageInfoLookup();
    }

    public static void UpdateEntityLookups(ref SystemState state, ref ComponentLookup<LocalTransform> localTransformLookup, ref EntityStorageInfoLookup entityStorageInfoLookup)
    {
        localTransformLookup.Update(ref state);
        entityStorageInfoLookup.Update(ref state);
    }



    /// <summary>
    /// Gets the CollisionWorld for an EntityManager, used for physics queries. 
    /// </summary>
    public static CollisionWorld GetCollisionWorld(this EntityManager em)
    {
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<PhysicsWorldSingleton>().Build(em);

        PhysicsWorldSingleton physiscsWorldSingleton = query.GetSingleton<PhysicsWorldSingleton>();

        CollisionWorld collisionWorld = physiscsWorldSingleton.CollisionWorld;

        return collisionWorld;
    }
}
