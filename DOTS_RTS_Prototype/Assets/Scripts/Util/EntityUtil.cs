using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

/// <summary>
/// Utility class for entities and their components.
/// </summary>
/// <remarks>
/// The component is a Singleton.
/// </remarks>
public static class EntityUtil
{
    static EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

    /// <summary>
    /// Checks if an entity currently valid against null values. . 
    /// </summary>
    /// <remarks>
    /// The method validates an entity checking if it actually exists and if it's queued for removal. If any of both conditions fail, returns false.
    /// This must be used in place of plain <see cref="EntityManager.Exists(Entity)"/>, since entities queued for removal returns true in said method.
    /// </remarks>
    public static bool ExistsAndPersists(this EntityManager em, Entity entity)
    {
        if (entity == Entity.Null)
        {
            return false;
        }
        if (!entityManager.Exists(entity))
        {
            return false;
        }
        if (!entityManager.HasComponent<LocalTransform>(entity))
        {
            return false;
        }
        return true;
    }
}
