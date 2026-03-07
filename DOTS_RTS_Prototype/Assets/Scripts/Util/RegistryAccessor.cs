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
    /// Gets the AnimationData entry in the registry's BlobArray.
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

        throw new System.Exception("AnimationKey not found in BlobArray: " + animationKey);
    }

    [BurstDiscard]
    private static void ThrowAnimationKeyNotFound(AnimationKey key)
    {
        Debug.Log($"AnimationKey not found: {key.name}");
        throw new System.Exception("AnimationKey not found in AnimationData blob: " + key.name);
    }
}
