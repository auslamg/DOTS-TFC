using Unity.Burst;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Provides binary-search utilities for retrieving <see cref="AnimationData"/> entries by key.
/// </summary>
[BurstCompile]
public class LookupAnimationData
{
    /// <summary>
    /// Retrieves an <see cref="AnimationData"/> entry from the registry blob array.
    /// </summary>
    /// <param name="animationDataBlobArrayRef">Blob array reference containing animation data entries sorted by key.</param>
    /// <param name="animationKey">Key used to find the desired animation entry.</param>
    /// <returns>A reference to the matched <see cref="AnimationData"/> entry.</returns>
    /// <remarks>The blob array must be sorted by <see cref="AnimationData.animationKey"/> for binary search to work correctly.</remarks>
    [BurstCompile]
    public static ref AnimationData GetAnimationData(
        ref BlobAssetReference<BlobArray<AnimationData>> animationDataBlobArrayRef,
        in AnimationKey animationKey)
    {
        AnimationKey searchKey = animationKey;

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
            //Cut the upper half out
            else
            {
                rightIndex = middleIndex - 1;
            }
        }

        if (searchKey.name != "")
        {
            LogErrorAnimationKeyNotFound(searchKey);
            Debug.LogError("AnimationKey not found in AnimationData blob. Disable Burst for details.");
        }
        return ref animationDataArray[0];
    }

    /// <summary>
    /// Logs a detailed error for a missing animation key when Burst is disabled.
    /// </summary>
    /// <param name="key">Missing animation key.</param>
    [BurstDiscard]
    private static void LogErrorAnimationKeyNotFound(AnimationKey key)
    {
        Debug.LogError("AnimationKey not found in AnimationData blob: " + key.name);
    }
}
