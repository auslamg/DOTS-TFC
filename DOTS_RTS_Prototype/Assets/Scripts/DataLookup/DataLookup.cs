using System;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Centralized facade for data lookup helpers used by ECS systems.
/// </summary>
/// <remarks>
/// Delegates binary-search lookups to specialized helpers through a single entry point.
/// </remarks>
[BurstCompile]
public static class DataLookup
{
    /// <summary>
    /// Retrieves an <see cref="AnimationData"/> entry from a sorted animation blob array.
    /// </summary>
    /// <param name="animationDataBlobArrayRef">Blob array reference containing animation registry entries sorted by key.</param>
    /// <param name="animationKey">Key used to locate the animation entry.</param>
    /// <returns>A reference to the matching <see cref="AnimationData"/> entry.</returns>
    [BurstCompile]
    public static ref AnimationData GetAnimationData(
        ref BlobAssetReference<BlobArray<AnimationData>> animationDataBlobArrayRef,
        in AnimationKey animationKey)
    {
        return ref LookupAnimationData.GetAnimationData(
            ref animationDataBlobArrayRef,
            animationKey);
    }

    /// <summary>
    /// Retrieves a <see cref="UnitData"/> entry from a sorted unit blob array.
    /// </summary>
    /// <param name="unitDataBlobArrayRef">Blob array reference containing unit registry entries sorted by key.</param>
    /// <param name="unitKey">Key used to locate the unit entry.</param>
    /// <returns>A reference to the matching <see cref="UnitData"/> entry.</returns>
    [BurstCompile]
    public static ref UnitData GetUnitData(
        ref BlobAssetReference<BlobArray<UnitData>> unitDataBlobArrayRef,
        in UnitKey unitKey)
    {
        return ref LookupUnitData.GetUnitData(
            ref unitDataBlobArrayRef,
            unitKey);
    }

    /// <summary>
    /// Retrieves a <see cref="BuildingData"/> entry from a sorted building blob array.
    /// </summary>
    /// <param name="buildingDataBlobArrayRef">Blob array reference containing building registry entries sorted by key.</param>
    /// <param name="buildingKey">Key used to locate the building entry.</param>
    /// <returns>A reference to the matching <see cref="BuildingData"/> entry.</returns>
    [BurstCompile]
    public static ref BuildingData GetBuildingData(
        ref BlobAssetReference<BlobArray<BuildingData>> buildingDataBlobArrayRef,
        in BuildingKey buildingKey)
    {
        return ref LookupBuildingData.GetBuildingData(
            ref buildingDataBlobArrayRef,
            buildingKey);
    }

    /// <summary>
    /// Retrieves an entity prefab from the supplied entity prefab buffer.
    /// </summary>
    /// <param name="entityReferencesBuffer">Sorted dynamic buffer of entity prefab mappings.</param>
    /// <param name="entityKey">Key used to locate the prefab entity.</param>
    /// <returns>The matched prefab entity.</returns>
    public static Entity GetEntityPrefab(
        ref DynamicBuffer<EntityPrefab> entityReferencesBuffer,
        in EntityPrefabKey entityKey)
    {
        return LookupEntityPrefab.GetEntityPrefab(
            ref entityReferencesBuffer,
            entityKey);
    }

    /// <summary>
    /// Attempts to retrieve an entity prefab from the supplied entity prefab buffer.
    /// </summary>
    /// <param name="entityReferencesBuffer">Sorted dynamic buffer of entity prefab mappings.</param>
    /// <param name="entityKey">Key used to locate the prefab entity.</param>
    /// <param name="prefabEntity">When this method returns, contains the matched entity or <see cref="Entity.Null"/>.</param>
    /// <returns><see langword="true"/> if a prefab was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetEntityPrefab(
        ref DynamicBuffer<EntityPrefab> entityReferencesBuffer,
        in EntityPrefabKey entityKey,
        out Entity prefabEntity)
    {
        return LookupEntityPrefab.TryGetEntityPrefab(
            ref entityReferencesBuffer,
            entityKey,
            out prefabEntity);
    }

    /// <summary>
    /// Attempts to retrieve an entity prefab from the default world using managed APIs.
    /// </summary>
    /// <param name="entityKey">Key used to locate the prefab entity.</param>
    /// <param name="prefabEntity">When this method returns, contains the matched entity or <see cref="Entity.Null"/>.</param>
    /// <returns><see langword="true"/> if a prefab was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryFetchEntityPrefab(
        in EntityPrefabKey entityKey,
        out Entity prefabEntity)
    {
        return LookupEntityPrefab.TryFetchEntityPrefab(
            entityKey,
            out prefabEntity);
    }

    /// <summary>
    /// Retrieves an entity prefab from the default world using managed APIs.
    /// </summary>
    /// <param name="entityKey">Key used to locate the prefab entity.</param>
    /// <returns>The matched prefab entity, or <see cref="Entity.Null"/> when not found.</returns>
    public static Entity FetchEntityPrefab(
        in EntityPrefabKey entityKey)
    {
        return LookupEntityPrefab.FetchEntityPrefab(
            entityKey);
    }
}

