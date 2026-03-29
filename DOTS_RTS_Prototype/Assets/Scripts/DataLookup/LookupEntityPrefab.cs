using System;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Provides binary-search lookup utilities for <see cref="EntityPrefab"/> mappings.
/// </summary>
[BurstCompile]
public class LookupEntityPrefab
{
    /// <summary>
    /// Attempts to get the index of an <see cref="EntityPrefab"/> entry in a sorted registry buffer.
    /// </summary>
    /// <param name="entityRefBuffer">Sorted dynamic buffer of entity prefab mappings.</param>
    /// <param name="entityKey">Key used to locate the prefab mapping.</param>
    /// <param name="entityRefIndex">When this method returns, contains the found index or <c>-1</c> when not found.</param>
    /// <returns><see langword="true"/> if the mapping was found; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetEntityPrefabIndex(
        ref DynamicBuffer<EntityPrefab> entityRefBuffer,
        in EntityPrefabKey entityKey,
        out int entityRefIndex)
    {
        //Start on the leftmost end, with a maximum of the total length
        int leftIndex = 0;
        int rightIndex = entityRefBuffer.Length - 1;

        while (leftIndex <= rightIndex)
        {
            //Get the middle index and check how it compares against the desired element
            int middleIndex = leftIndex + (rightIndex - leftIndex) / 2;
            int comparisonResult = entityRefBuffer[middleIndex].entityRefKey.CompareTo(entityKey);

            //Element found
            if (comparisonResult == 0)
            {
                entityRefIndex = middleIndex;
                return true;
            }

            //Cut the lower half out
            if (comparisonResult < 0)
            {
                leftIndex = middleIndex + 1;
            }
            //Cut the upper half out
            else
            {
                rightIndex = middleIndex - 1;
            }
        }

        entityRefIndex = -1;
        return false;
    }

    /// <summary>
    /// Retrieves the index of an <see cref="EntityPrefab"/> entry in a sorted registry buffer.
    /// </summary>
    /// <param name="entityRefBuffer">Sorted dynamic buffer of entity prefab mappings.</param>
    /// <param name="entityKey">Key used to locate the prefab mapping.</param>
    /// <returns>The index of the matched mapping, or <c>-1</c> when not found.</returns>
    private static int GetEntityPrefabIndex(
        ref DynamicBuffer<EntityPrefab> entityRefBuffer,
        in EntityPrefabKey entityKey)
    {
        //Start on the leftmost end, with a maximum of the total length
        int leftIndex = 0;
        int rightIndex = entityRefBuffer.Length - 1;

        while (leftIndex <= rightIndex)
        {
            //Get the middle index and check how it compares against the desired element
            int middleIndex = leftIndex + (rightIndex - leftIndex) / 2;
            int comparisonResult = entityRefBuffer[middleIndex].entityRefKey.CompareTo(entityKey);

            //Element found
            if (comparisonResult == 0)
            {
                return middleIndex;
            }

            //Cut the lower half out
            if (comparisonResult < 0)
            {
                leftIndex = middleIndex + 1;
            }
            //Cut the upper half out
            else
            {
                rightIndex = middleIndex - 1;
            }
        }
        LogErrorEntityReferenceKeyNotFound(entityKey);
        Debug.LogError("EntityReferenceKey not found in EntityReference buffer. Disable Burst for details.");
        return -1;
    }

    /// <summary>
    /// Attempts to retrieve a prefab entity from a provided registry buffer.
    /// </summary>
    /// <param name="entityReferencesBuffer">Sorted dynamic buffer of entity prefab mappings.</param>
    /// <param name="entityKey">Key used to locate the prefab mapping.</param>
    /// <param name="prefabEntity">When this method returns, contains the prefab entity or <see cref="Entity.Null"/>.</param>
    /// <returns><see langword="true"/> if a prefab entity was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetEntityPrefab(
        ref DynamicBuffer<EntityPrefab> entityReferencesBuffer,
        in EntityPrefabKey entityKey,
        out Entity prefabEntity)
    {
        if (TryGetEntityPrefabIndex(ref entityReferencesBuffer, entityKey, out int entityRefIndex))
        {
            prefabEntity = entityReferencesBuffer[entityRefIndex].prefabEntity;
            return true;
        }

        prefabEntity = Entity.Null;
        return false;
    }
    
    /// <summary>
    /// Retrieves a prefab entity from the provided registry buffer.
    /// </summary>
    /// <param name="entityReferencesBuffer">Sorted dynamic buffer of entity prefab mappings.</param>
    /// <param name="entityKey">Key used to locate the prefab mapping.</param>
    /// <returns>The matched prefab entity.</returns>
    public static Entity GetEntityPrefab(
        ref DynamicBuffer<EntityPrefab> entityReferencesBuffer,
        in EntityPrefabKey entityKey)
    {
        int entityRefIndex = GetEntityPrefabIndex(ref entityReferencesBuffer, entityKey);
        EntityPrefab entityReference = entityReferencesBuffer[entityRefIndex];
        Entity prefabEntity = entityReference.prefabEntity;
        return prefabEntity;
    }

    /// <summary>
    /// Attempts to retrieve a prefab entity from the default world's singleton registry.
    /// </summary>
    /// <param name="entityKey">Key used to locate the prefab mapping.</param>
    /// <param name="prefabEntity">When this method returns, contains the prefab entity or <see cref="Entity.Null"/>.</param>
    /// <returns><see langword="true"/> if a prefab entity was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryFetchEntityPrefab(
        in EntityPrefabKey entityKey,
        out Entity prefabEntity)
    {
        try
        {
            prefabEntity = FetchEntityPrefab(entityKey);
            if (prefabEntity != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"EXCEPTION: {e.Message}");
            prefabEntity = Entity.Null;
            return false;
        }
    }

    /// <summary>
    /// Retrieves a prefab entity from the default world's singleton registry.
    /// </summary>
    /// <param name="entityKey">Key used to locate the prefab mapping.</param>
    /// <returns>The matched prefab entity, or <see cref="Entity.Null"/> when unavailable.</returns>
    public static Entity FetchEntityPrefab(
        in EntityPrefabKey entityKey)
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityQuery query = entityManager.CreateEntityQuery(typeof(EntityPrefabsRegistry));
        if (query.CalculateEntityCount() == 0)
        {
            return Entity.Null;
        }

        Entity singletonEntity = query.GetSingletonEntity();
        DynamicBuffer<EntityPrefab> entityReferencesBuffer = entityManager.GetBuffer<EntityPrefab>(singletonEntity);

        if (TryGetEntityPrefabIndex(ref entityReferencesBuffer, entityKey, out int entityRefIndex))
        {
            return entityReferencesBuffer[entityRefIndex].prefabEntity;
        }
        else
        {
            return Entity.Null;
        }
    }

    /// <summary>
    /// Logs a detailed error for a missing entity prefab key when Burst is disabled.
    /// </summary>
    /// <param name="key">Missing entity prefab key.</param>
    [BurstDiscard]
    private static void LogErrorEntityReferenceKeyNotFound(EntityPrefabKey key)
    {
        Debug.LogError("EntityReferenceKey not found in EntityReference buffer: " + key.name);
    }
}
