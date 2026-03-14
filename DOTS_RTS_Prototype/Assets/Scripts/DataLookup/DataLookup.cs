using System;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Centralized facade for data lookup helpers used by ECS systems.
/// </summary>
/// <remarks>
/// Delegates lookups to specialized classes so call sites can use a single entry point.
/// </remarks>
[BurstCompile]
public static class DataLookup
{
    /// <summary>
    /// Gets an <see cref="AnimationData"/> record from a sorted animation blob array.
    /// </summary>
    /// <param name="animationDataBlobArrayRef">Blob array reference containing animation registry data.</param>
    /// <param name="animationKey">Key used to locate the animation data record.</param>
    /// <returns>A reference to the matching <see cref="AnimationData"/> record.</returns>
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
    /// Gets a <see cref="UnitData"/> record from a sorted unit blob array.
    /// </summary>
    /// <param name="unitDataBlobArrayRef">Blob array reference containing unit registry data.</param>
    /// <param name="unitKey">Key used to locate the unit data record.</param>
    /// <returns>A reference to the matching <see cref="UnitData"/> record.</returns>
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
    /// Gets a <see cref="BuildingData"/> record from a sorted building blob array.
    /// </summary>
    /// <param name="buildingDataBlobArrayRef">Blob array reference containing building registry data.</param>
    /// <param name="buildingKey">Key used to locate the building data record.</param>
    /// <returns>A reference to the matching <see cref="BuildingData"/> record.</returns>
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
    /// Gets an entity prefab from the supplied entity prefab buffer.
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
    /// Attempts to get an entity prefab from the supplied entity prefab buffer.
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
    /// Attempts to fetch an entity prefab from the default world using managed APIs.
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
    /// Fetches an entity prefab from the default world using managed APIs.
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

