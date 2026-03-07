using System.Collections.Generic;
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

        //Allocate array space for each individual animation according to its frame count
        AnimationDataRegistrySO animationRegistry =
            SystemAPI.GetSingleton<AnimationRegistrySOReference>().registry;
        //FIX: Convert to RefRO's
        foreach (AnimationDataSO animation in animationRegistry.animationDataSOList) 
        {
            animationFramesDictionary[animation.animationKey] = new int[animation.meshArray.Length];
        }

        //Fill up the dictionary entries with the relevant MaterialMeshInfo, retrieved from meshBakingEntity entities created during baking process.
        ///See <see cref="AnimationDataRegistryBaker"/>
        foreach ((
            RefRO<AnimationFrameMetadata> animationFrameMetadata,
            RefRW<MaterialMeshInfo> materialMeshInfo)
                in SystemAPI.Query<
                RefRO<AnimationFrameMetadata>,
                RefRW<MaterialMeshInfo>>())
        {
            animationFramesDictionary[animationFrameMetadata.ValueRO.animationKey][animationFrameMetadata.ValueRO.meshIndex] =
                materialMeshInfo.ValueRO.Mesh;

            //Debug logging for baking info
            /* Debug.Log(
                "Baked animation frame mesh:\t" +
                animationDataRegistryFrameMetadata.ValueRO.animationKey +
                " :: " + animationDataRegistryFrameMetadata.ValueRO.meshIndex +
                " = " + materialMeshInfo.ValueRO.Mesh); */
        }

        // Build the BlobAssetReference that will store all animation data in a contiguous,
        // Burst-compatible memory structure. The blob will contain a BlobArray<AnimationData>,
        // where each AnimationData entry corresponds to one AnimationDataSO in the registry.
        
        // This blob is attached to the singleton AnimationDataRegistry entity and will be used
        // at runtime for extremely fast animation lookup without requiring managed memory,
        // ScriptableObject access, or entity queries.
        RefRW<AnimationDataRegistry> animationDataRegistry = SystemAPI.GetSingletonRW<AnimationDataRegistry>();
        {
            //Build new blob root through BlobBuilder
            BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);
            ref BlobArray<AnimationData> animationDataBlobRoot = ref blobBuilder.ConstructRoot<BlobArray<AnimationData>>();

            //Allocate memory for AnimationData array in the constructed root BlobArray
            int animationRegistrySize = animationRegistry.animationDataSOList.Count;
            BlobBuilderArray<AnimationData> animationDataBlobEntries =
                blobBuilder.Allocate<AnimationData>(ref animationDataBlobRoot, animationRegistrySize);

            //Process all AnimationData ScriptableObjects found in the registry's list
            int animationSOIndex = 0;
            foreach (AnimationDataSO animationDataSO in animationRegistry.animationDataSOList)
            {
                //Allocate memory for the mesh array
                BlobBuilderArray<int> frameMeshIds =
                    blobBuilder.Allocate<int>(ref animationDataBlobEntries[animationSOIndex].frameMeshIdIndex, animationDataSO.meshArray.Length);

                //Edit singular data inside blob
                animationDataBlobEntries[animationSOIndex].animationKey = animationDataSO.animationKey;
                animationDataBlobEntries[animationSOIndex].playFull = animationDataSO.playFull;
                animationDataBlobEntries[animationSOIndex].frameFrequency = animationDataSO.frameFrequency;
                animationDataBlobEntries[animationSOIndex].frameCount = animationDataSO.meshArray.Length;

                //Register all meshes in the mesh array
                for (int i = 0; i < animationDataSO.meshArray.Length; i++)
                {
                    //Add to Blob baked mesh from dictionary
                    frameMeshIds[i] = animationFramesDictionary[animationDataSO.animationKey][i];
                }

                animationSOIndex++;
            }

            //Build BlobAssetReference
            animationDataRegistry.ValueRW.animationDataBlobArrayReference =
                blobBuilder.CreateBlobAssetReference<BlobArray<AnimationData>>(Allocator.Persistent);

            //Dispose resources to avoid memory leaks
            blobBuilder.Dispose();
        }
    }
}
