using Unity.Burst;
using Unity.Entities;
using UnityEngine;

/// <summary>
/// Provides binary-search utilities for retrieving <see cref="UnitData"/> entries by key.
/// </summary>
[BurstCompile]
public class LookupUnitData
{
    /// <summary>
    /// Retrieves a <see cref="UnitData"/> entry from the registry blob array.
    /// </summary>
    /// <param name="unitDataBlobArrayRef">Blob array reference containing unit data entries sorted by key.</param>
    /// <param name="unitKey">Key used to find the desired unit entry.</param>
    /// <returns>A reference to the matched <see cref="UnitData"/> entry.</returns>
    /// <remarks>The blob array must be sorted by <see cref="UnitData.unitKey"/> for binary search to work correctly.</remarks>
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
            //Cut the upper half out
            else
            {
                rightIndex = middleIndex - 1;
            }
        }

        LogErrorUnitKeyNotFound(unitKey);
        Debug.LogError("UnitKey not found in UnitData blob. Disable Burst for details.");
        return ref unitDataArray[0];
    }

    /// <summary>
    /// Logs a detailed error for a missing unit key when Burst is disabled.
    /// </summary>
    /// <param name="key">Missing unit key.</param>
    [BurstDiscard]
    private static void LogErrorUnitKeyNotFound(UnitKey key)
    {
        Debug.LogError("UnitKey not found in UnitData blob: " + key.name);
    }
}
