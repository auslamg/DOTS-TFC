using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// Utility class for entities and their components.
/// </summary>
/// <remarks>
/// The component is a Singleton.
/// </remarks>
[BurstCompile]
public static class EntityUtil
{
    /// <summary>
    /// Validates a that an Entity exists.
    /// </summary>
    /// <remarks>
    /// The method validates an entity checking if it actually exists and if it's queued for removal (by checking if it contains a LocalTransform component). If any of both conditions fail, returns false.
    /// This must be used in place of plain <see cref="EntityManager.Exists(Entity)"/>, since entities queued for removal returns true in said method.
    /// </remarks>
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
    /// Validates a that an Entity exists.
    /// </summary>
    /// <remarks>
    /// The method validates an entity checking if it actually exists and if it's queued for removal (by checking if it contains a LocalTransform component). If any of both conditions fail, returns false.
    /// This must be used in place of plain <see cref="EntityManager.Exists(Entity)"/>, since entities queued for removal returns true in said method.
    /// </remarks>
    [BurstCompile]
    public static bool ExistsAndPersists(ref SystemState state, in Entity entity)
    {
        EntityManager em = state.EntityManager;
        if (entity == Entity.Null)
        {
            Debug.Log("Entity is null");
            return false;
        }
        if (!em.Exists(entity))
        {
            Debug.Log("Entity doesn't exist in em");
            return false;
        }
        if (!em.HasComponent<LocalTransform>(entity))
        {
            Debug.Log("Entity doesn'thave LocalTransform");
            return false;
        }
        return true;
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

    /// <summary>
    /// Gets the AnimationData entry in the registry's BlobArray. 
    /// </summary>
    /// //TODO: CHeck burst
    public static ref AnimationData GetAnimationData(
    ref BlobAssetReference<BlobArray<AnimationData>> blobRef,
    in AnimationKey key)
    {
        ref var array = ref blobRef.Value;

        for (int i = 0; i < array.Length; i++)
        {
            if (array[i].animationKey.Equals(key))
            {
                return ref array[i];
            }
        }

        // Note: 
        // This should never happen as long as introduced keys are always valid
        throw new System.Exception("AnimationKey not found in AnimationData blob: " + key.name);
    }
}
