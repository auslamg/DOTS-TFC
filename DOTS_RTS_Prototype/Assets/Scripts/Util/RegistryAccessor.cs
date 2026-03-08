using Unity.Burst;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Utility class for accessing ScripableObject registry entries.
/// </summary>
[BurstCompile]
public static class RegistryAccessor
{
    /// <summary>
    /// Gets the <see cref="AnimationData"/> entry in the registry's BlobArray.
    /// Employs a binary sort pattern.
    /// </summary>
    [BurstCompile]
    public static ref AnimationData GetAnimationData(
    ref BlobAssetReference<BlobArray<AnimationData>> animationDataBlobArrayRef,
    in AnimationKey animationKey)
    {
        AnimationKey searchKey = animationKey;

        if (searchKey.name == "")
        {
            searchKey.name = "None";
        }

        ref BlobArray<AnimationData> animationDataArray = ref animationDataBlobArrayRef.Value;

        //Start on the leftmost end, with a maximum of the total length
        int leftIndex = 0;
        int rightIndex = animationDataArray.Length - 1;

        while (leftIndex <= rightIndex)
        {
            //Get the middle index and check how it compares against the desired element
            int middleIndex = leftIndex + (rightIndex - leftIndex) / 2;
            int comparisonResult = animationDataArray[middleIndex].animationKey.CompareTo(searchKey);

            //Element found
            if (comparisonResult == 0)
            {
                return ref animationDataArray[middleIndex];
            }

            //Cut the lower half out
            if (comparisonResult < 0)
            {
                leftIndex = middleIndex + 1;
            }
            //Cut the the upper half out
            else
            {
                rightIndex = middleIndex - 1;
            }
        }

        LogErrorAnimationKeyNotFound(searchKey);
        Debug.LogError("AnimationKey not found in BlobArray. Disable Burst for details.");
        return ref animationDataArray[0];
    }

    [BurstDiscard]
    private static void LogErrorAnimationKeyNotFound(AnimationKey key)
    {
        Debug.LogError("AnimationKey not found in AnimationData blob: " + key.name);
    }

    /// <summary>
    /// Gets the <see cref="BuildingData"/> entry in the registry's BlobArray.
    /// Employs a binary sort pattern.
    /// </summary>
    [BurstCompile]
    public static ref BuildingData GetBuildingData(
    ref BlobAssetReference<BlobArray<BuildingData>> buildingDataBlobArrayRef,
    in BuildingKey buildingKey)
    {
        ref BlobArray<BuildingData> buildingDataArray = ref buildingDataBlobArrayRef.Value;

        //Start on the leftmost end, with a maximum of the total length
        int leftIndex = 0;
        int rightIndex = buildingDataArray.Length - 1;

        while (leftIndex <= rightIndex)
        {
            //Get the middle index and check how it compares against the desired element
            int middleIndex = leftIndex + (rightIndex - leftIndex) / 2;
            int comparisonResult = buildingDataArray[middleIndex].buildingKey.CompareTo(buildingKey);

            //Element found
            if (comparisonResult == 0)
            {
                return ref buildingDataArray[middleIndex];
            }

            //Cut the lower half out
            if (comparisonResult < 0)
            {
                leftIndex = middleIndex + 1;
            }
            //Cut the the upper half out
            else
            {
                rightIndex = middleIndex - 1;
            }
        }

        LogErrorBuildingKeyNotFound(buildingKey);
        Debug.LogError("BuildingKey not found in BlobArray. Disable Burst for details.");
        return ref buildingDataArray[0];
    }

    [BurstDiscard]
    private static void LogErrorBuildingKeyNotFound(BuildingKey key)
    {
        Debug.LogError("BuildingKey not found in BuildingData blob: " + key.name);
    }

    /// <summary>
    /// Gets the <see cref="UnitData"/> entry in the registry's BlobArray.
    /// Employs a binary sort pattern.
    /// </summary>
    [BurstCompile]
    public static ref UnitData GetUnitData(
    ref BlobAssetReference<BlobArray<UnitData>> unitDataBlobArrayRef,
    in UnitKey unitKey)
    {
        ref BlobArray<UnitData> unitDataArray = ref unitDataBlobArrayRef.Value;

        //Start on the leftmost end, with a maximum of the total length
        int leftIndex = 0;
        int rightIndex = unitDataArray.Length - 1;

        while (leftIndex <= rightIndex)
        {
            //Get the middle index and check how it compares against the desired element
            int middleIndex = leftIndex + (rightIndex - leftIndex) / 2;
            int comparisonResult = unitDataArray[middleIndex].unitKey.CompareTo(unitKey);

            //Element found
            if (comparisonResult == 0)
            {
                return ref unitDataArray[middleIndex];
            }

            //Cut the lower half out
            if (comparisonResult < 0)
            {
                leftIndex = middleIndex + 1;
            }
            //Cut the the upper half out
            else
            {
                rightIndex = middleIndex - 1;
            }
        }

        LogErrorUnitKeyNotFound(unitKey);
        Debug.LogError("UnitKey not found in BlobArray. Disable Burst for details.");
        return ref unitDataArray[0];
    }

    [BurstDiscard]
    private static void LogErrorUnitKeyNotFound(UnitKey key)
    {
        Debug.LogError("UnitKey not found in UnitData blob: " + key.name);
    }

    /// <summary>
    /// Gets the <see cref="EntityReference"/> entry in the registry's BlobArray.
    /// Employs a binary sort pattern.
    /// </summary>
    [BurstCompile]
    public static ref EntityReference GetEntityReference(
    ref BlobAssetReference<BlobArray<EntityReference>> entityRefBlobArrayRef,
    in EntityReferenceKey entityKey)
    {
        ref BlobArray<EntityReference> entityRefArray = ref entityRefBlobArrayRef.Value;

        //Start on the leftmost end, with a maximum of the total length
        int leftIndex = 0;
        int rightIndex = entityRefArray.Length - 1;

        while (leftIndex <= rightIndex)
        {
            //Get the middle index and check how it compares against the desired element
            int middleIndex = leftIndex + (rightIndex - leftIndex) / 2;
            int comparisonResult = entityRefArray[middleIndex].entityKey.CompareTo(entityKey);

            //Element found
            if (comparisonResult == 0)
            {
                return ref entityRefArray[middleIndex];
            }

            //Cut the lower half out
            if (comparisonResult < 0)
            {
                leftIndex = middleIndex + 1;
            }
            //Cut the the upper half out
            else
            {
                rightIndex = middleIndex - 1;
            }
        }

        LogErrorEntityReferenceKeyNotFound(entityKey);
        Debug.LogError("EntityReferenceKey not found in BlobArray. Disable Burst for details.");
        throw new System.Exception("Tried to spawn no units");
    }

    [BurstDiscard]
    private static void LogErrorEntityReferenceKeyNotFound(EntityReferenceKey key)
    {
        Debug.LogError("EntityReferenceKey not found in UnitData blob: " + key.name);
    }
}
