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
        ref BlobArray<AnimationData> animationDataArray = ref animationDataBlobArrayRef.Value;

        //Start on the leftmost end, with a maximum of the total length
        int leftIndex = 0;
        int rightIndex = animationDataArray.Length - 1;

        while (leftIndex <= rightIndex)
        {
            //Get the middle index and check how it compares against the desired element
            int middleIndex = leftIndex + (rightIndex - leftIndex) / 2;
            int comparisonResult = animationDataArray[middleIndex].animationKey.CompareTo(animationKey);

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

        ThrowAnimationKeyNotFound(animationKey);
        throw new System.Exception("AnimationKey not found in BlobArray. Disable Burst for details.");
    }

    [BurstDiscard]
    private static void ThrowAnimationKeyNotFound(AnimationKey key)
    {
        throw new System.Exception("AnimationKey not found in AnimationData blob: " + key.name);
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

        ThrowBuildingKeyNotFound(buildingKey);
        throw new System.Exception("BuildingKey not found in BlobArray. Disable Burst for details.");
    }

    [BurstDiscard]
    private static void ThrowBuildingKeyNotFound(BuildingKey key)
    {
        throw new System.Exception("BuildingKey not found in BuildingData blob: " + key.name);
    }
}
