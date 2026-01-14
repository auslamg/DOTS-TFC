using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
[UpdateInGroup(typeof(PostBakingSystemGroup))]
// IDEA: Require matching queries for update
partial struct AnimationDataHolderBakingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        //Create dictionary for AnimationKey :: MeshID (int) employed for animation mesh queries
        Dictionary<AnimationKey, int[]> blobAssetDataDictionary = new Dictionary<AnimationKey, int[]>();

        //Allocate array space for each individual animation according to its frame count
        AnimationDataRegistrySO animationDataRegistrySO =
            SystemAPI.GetSingleton<AnimationRegistryReference>().registry;
        foreach (AnimationDataSO animationDataSO in animationDataRegistrySO.animationDataSOList) //FIX: Convert to RefRO's
        {
            blobAssetDataDictionary[animationDataSO.animationKey] = new int[animationDataSO.meshArray.Length];
        }

        //Fill up the dictionary entries with the relevant materialMeshInfo
        foreach ((
        RefRO<AnimationDataHolderSubEntity> animationDataHolderSubEntity,
        RefRW<MaterialMeshInfo> materialMeshInfo)
            in SystemAPI.Query<
            RefRO<AnimationDataHolderSubEntity>,
            RefRW<MaterialMeshInfo>>())
        {
            blobAssetDataDictionary[animationDataHolderSubEntity.ValueRO.animationKey][animationDataHolderSubEntity.ValueRO.meshIndex] =
                materialMeshInfo.ValueRO.Mesh;

            Debug.Log(
                "Baked animation frame mesh:\t" +
                animationDataHolderSubEntity.ValueRO.animationKey +
                " :: " + animationDataHolderSubEntity.ValueRO.meshIndex +
                " = " + materialMeshInfo.ValueRO.Mesh);
        }

        //REVIEW: This might just run once, making the foreach pointless
        //Construct blob builder
        foreach (RefRW<AnimationDataHolder> animationDataHolder in SystemAPI.Query<RefRW<AnimationDataHolder>>())
        {
            //Build new blob root
            BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);
            ref BlobArray<AnimationData> animationDataBlobArray = ref blobBuilder.ConstructRoot<BlobArray<AnimationData>>();

            //Allocate memory for AnimationData array
            int blobArraySize = animationDataRegistrySO.animationDataSOList.Count;
            BlobBuilderArray<AnimationData> animationDataBlobBuilderArray =
                blobBuilder.Allocate<AnimationData>(ref animationDataBlobArray, blobArraySize);

            //For all Animation ScriptableObjects found in the list reader
            int animationSOIndex = 0;
            foreach (AnimationDataSO animationDataSO in animationDataRegistrySO.animationDataSOList) //FIX might be unnecessary to loop twice
            {
                //Allocate memory for the mesh array
                BlobBuilderArray<int> blobBuilderArray =
                    blobBuilder.Allocate<int>(ref animationDataBlobBuilderArray[animationSOIndex].intMeshIdBlobArray, animationDataSO.meshArray.Length);

                //Edit singular data inside blob
                animationDataBlobBuilderArray[animationSOIndex].animationKey = animationDataSO.animationKey;
                animationDataBlobBuilderArray[animationSOIndex].playFull = animationDataSO.playFull;
                animationDataBlobBuilderArray[animationSOIndex].frameFrequency = animationDataSO.frameFrequency;
                animationDataBlobBuilderArray[animationSOIndex].frameCount = animationDataSO.meshArray.Length;

                //Register all meshes in the mesh array
                for (int i = 0; i < animationDataSO.meshArray.Length; i++)
                {
                    //Add to BlobBuilder baked mesh from dictionary
                    blobBuilderArray[i] = blobAssetDataDictionary[animationDataSO.animationKey][i];
                }

                animationSOIndex++;
            }

            //Build blobAssetReference
            animationDataHolder.ValueRW.animationDataBlobArrayAssetReference =
                blobBuilder.CreateBlobAssetReference<BlobArray<AnimationData>>(Allocator.Persistent);

            //Dispose resources to avoid memory leaks
            blobBuilder.Dispose();
        }
    }
}
