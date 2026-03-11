using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[UpdateInGroup(typeof(PostBakingSystemGroup))]
// IDEA: Require matching queries for update
partial struct AnimationDataRegistryPostBakingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        //Temporary Dictionary for AnimationKey :: MeshID (int) employed for animation mesh queries
        Dictionary<AnimationKey, int[]> animationFramesDictionary = new Dictionary<AnimationKey, int[]>();

        // Get the registry SO
        AnimationDataRegistrySO animationRegistry =
            SystemAPI.GetSingleton<AnimationRegistrySOReference>().registry;

        // Allocate arrays for each animation key
        foreach (AnimationDataSO animation in animationRegistry.animationDataSOList)
        {
            animationFramesDictionary[animation.animationKey] = new int[animation.meshArray.Length];
        }

        // Fill dictionary with Mesh IDs from the baked entities
        ///See <see cref="AnimationDataRegistryBaker"/>
        foreach ((
            RefRO<AnimationFrameMetadata> frameMetadata,
            RefRW<MaterialMeshInfo> meshInfo)
                in SystemAPI.Query<
                RefRO<AnimationFrameMetadata>,
                RefRW<MaterialMeshInfo>>())
        {
            animationFramesDictionary[frameMetadata.ValueRO.animationKey][frameMetadata.ValueRO.meshIndex] =
                meshInfo.ValueRO.Mesh;

            //Debug logging for baking info
            /* Debug.Log(
                "Baked animation frame mesh:\t" +
                animationDataRegistryFrameMetadata.ValueRO.animationKey +
                " :: " + animationDataRegistryFrameMetadata.ValueRO.meshIndex +
                " = " + materialMeshInfo.ValueRO.Mesh); */
        }

        // --- SORTED BLOB ARRAY LOGIC ---
        // Sort the AnimationDataSO list by AnimationKey
        AnimationDataSO[] sortedAnimations = animationRegistry.animationDataSOList
            .OrderBy((AnimationDataSO a) => a.animationKey)
            .ToArray();

        // Build the BlobAssetReference that will store all animation data in a contiguous,
        // Burst-compatible memory structure. The blob will contain a BlobArray<AnimationData>,
        // where each AnimationData entry corresponds to one AnimationDataSO in the registry.

        // This blob is attached to the singleton AnimationDataRegistry entity and will be used
        // at runtime for extremely fast animation lookup without requiring managed memory,
        // ScriptableObject access, or entity queries.
        RefRW<AnimationDataRegistry> animationDataRegistry = SystemAPI.GetSingletonRW<AnimationDataRegistry>();
        {
            //BlobBuilder resources
            using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
            {
                //Build new blob root
                ref BlobArray<AnimationData> root = ref blobBuilder.ConstructRoot<BlobArray<AnimationData>>();

                //Allocate memory for AnimationData array in the root
                int animationCount = sortedAnimations.Length;
                BlobBuilderArray<AnimationData> animationDataBlobEntries =
                    blobBuilder.Allocate<AnimationData>(ref root, animationCount);

                //Process all AnimationData ScriptableObjects found in the registry's list
                for (int animationIndex = 0; animationIndex < animationCount; animationIndex++)
                {
                    AnimationDataSO animationDataSO = sortedAnimations[animationIndex];
                    /* Debug.Log($"PostBaking animation data meshes: {animationDataSO.name}"); */

                    //Allocate memory for the mesh array in the AnimationData entry
                    BlobBuilderArray<int> frameMeshIds =
                        blobBuilder.Allocate<int>(ref animationDataBlobEntries[animationIndex].frameMeshIdIndex, animationDataSO.meshArray.Length);

                    //Bake singular data inside blob entry
                    animationDataBlobEntries[animationIndex].animationKey = animationDataSO.animationKey;
                    animationDataBlobEntries[animationIndex].animationType = animationDataSO.animationType;
                    animationDataBlobEntries[animationIndex].playFull = animationDataSO.playFull;
                    animationDataBlobEntries[animationIndex].frameFrequency = animationDataSO.frameFrequency;
                    animationDataBlobEntries[animationIndex].frameCount = animationDataSO.meshArray.Length;

                    //Register all frame meshes in the mesh array
                    for (int frameIndex = 0; frameIndex < animationDataSO.meshArray.Length; frameIndex++)
                    {
                        /* Debug.Log($"Added animation Key {animationDataSO.animationKey} mesh: {frameIndex}"); */

                        //Add to Blob baked mesh from dictionary
                        frameMeshIds[frameIndex] = animationFramesDictionary[animationDataSO.animationKey][frameIndex];
                    }
                }

                //Build BlobAssetReference
                animationDataRegistry.ValueRW.animationDataBlobArrayReference =
                    blobBuilder.CreateBlobAssetReference<BlobArray<AnimationData>>(Allocator.Persistent);
            }
        }
    }
}
