using Unity.Burst;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Provides binary-search utilities for retrieving <see cref="ResourceData"/> entries by key.
/// </summary>
[BurstCompile]
public static class LookupResourceData
{
    /// <summary>
    /// Retrieves a <see cref="ResourceData"/> entry from the registry blob array.
    /// </summary>
    /// <param name="resourceDataBlobArrayRef">Blob array reference containing resource data entries sorted by key.</param>
    /// <param name="resourceKey">Key used to find the desired resource entry.</param>
    /// <returns>A reference to the matched <see cref="ResourceData"/> entry.</returns>
    /// <remarks>The blob array must be sorted by <see cref="ResourceData.resourceKey"/> for binary search to work correctly.</remarks>
    [BurstCompile]
    public static ref ResourceData GetResourceData(
        ref BlobAssetReference<BlobArray<ResourceData>> resourceDataBlobArrayRef,
        in ResourceKey resourceKey)
    {
        ref BlobArray<ResourceData> resourceDataArray = ref resourceDataBlobArrayRef.Value;

        //Start on the leftmost end, with a maximum of the total length
        int leftIndex = 0;
        int rightIndex = resourceDataArray.Length - 1;

        while (leftIndex <= rightIndex)
        {
            //Get the middle index and check how it compares against the desired element
            int middleIndex = leftIndex + (rightIndex - leftIndex) / 2;
            int comparisonResult = resourceDataArray[middleIndex].resourceKey.CompareTo(resourceKey);

            //Element found
            if (comparisonResult == 0)
            {
                return ref resourceDataArray[middleIndex];
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

        LogErrorResourceKeyNotFound(resourceKey);
        Debug.LogError("ResourceKey not found in ResourceData blob. Disable Burst for details.");
        return ref resourceDataArray[0];
    }

    /// <summary>
    /// Logs a detailed error for a missing resource key when Burst is disabled.
    /// </summary>
    /// <param name="key">Missing resource key.</param>
    [BurstDiscard]
    private static void LogErrorResourceKeyNotFound(ResourceKey key)
    {
        Debug.LogError("ResourceKey not found in ResourceData blob: " + key.name);
    }
}