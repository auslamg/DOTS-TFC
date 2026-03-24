using Unity.Burst;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Provides lookup utilities for retrieving <see cref="BuildingData"/> records by key.
/// </summary>
[BurstCompile]
public class LookupBuildingData
{
    /// <summary>
    /// Gets the <see cref="BuildingData"/> entry in the registry blob array.
    /// </summary>
    /// <param name="buildingDataBlobArrayRef">Blob array reference containing building data entries sorted by key.</param>
    /// <param name="buildingKey">Key used to find the desired building entry.</param>
    /// <returns>A reference to the matched <see cref="BuildingData"/> entry.</returns>
    /// <remarks>The blob array must be sorted by <see cref="BuildingData.buildingKey"/> for binary search to work correctly.</remarks>
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
        Debug.LogError("BuildingKey not found in BuildingData blob. Disable Burst for details.");
        return ref buildingDataArray[0];
    }

    /// <summary>
    /// Logs a detailed error for a missing building key when Burst is disabled.
    /// </summary>
    /// <param name="key">Missing building key.</param>
    [BurstDiscard]
    private static void LogErrorBuildingKeyNotFound(BuildingKey key)
    {
        Debug.LogError("BuildingKey not found in BuildingData blob: " + key.name);
    }
}
