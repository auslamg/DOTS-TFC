using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

class AnimationDataHolderAuthoring : MonoBehaviour
{
    public AnimationDataSO soldierIdle;
    public AnimationDataSO soldierWalk;

    public static AnimationDataHolderAuthoring Instance { get; private set; }

    /// <summary>
    /// Used for singleton logic.
    /// </summary>
    void Awake()
    {
        //Singleton logic
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple instances of singleton found on " + this.gameObject.name);
            Destroy(this);
        }
    }
}

class AnimationDataBaker : Baker<AnimationDataHolderAuthoring>
{
    public override void Bake(AnimationDataHolderAuthoring authoring)
    {
        EntitiesGraphicsSystem egs =
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EntitiesGraphicsSystem>();

        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AnimationDataHolder animationDataHolder = new AnimationDataHolder();

        {
            //Build new blob 
            BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);
            ref AnimationData animationData = ref blobBuilder.ConstructRoot<AnimationData>();

            //Edit singular data inside blob
            animationData.frameFrequency = authoring.soldierIdle.frameFrequency;
            animationData.frameCount = authoring.soldierIdle.meshArray.Length;

            //Allocate memory for the array
            BlobBuilderArray<BatchMeshID> blobBuilderArray =
                blobBuilder.Allocate(ref animationData.batchMeshIdBlobArray, authoring.soldierIdle.meshArray.Length);

            for (int i = 0; i < authoring.soldierIdle.meshArray.Length; i++)
            {
                Mesh mesh = authoring.soldierIdle.meshArray[i];
                blobBuilderArray[i] =
                    egs.RegisterMesh(mesh);
            }

            //Build blobAssetReference
            animationDataHolder.soldierIdle = blobBuilder.CreateBlobAssetReference<AnimationData>(Allocator.Persistent);

            //Dispose resources to avoid memory leaks
            blobBuilder.Dispose();
            //Register one usage to avoid deallocation
            AddBlobAsset(ref animationDataHolder.soldierIdle, out Unity.Entities.Hash128 objectHash);
        }

        {
            //Build new blob 
            BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);
            ref AnimationData animationData = ref blobBuilder.ConstructRoot<AnimationData>();

            //Edit singular data inside blob
            animationData.frameFrequency = authoring.soldierWalk.frameFrequency;
            animationData.frameCount = authoring.soldierWalk.meshArray.Length;

            //Allocate memory for the array
            BlobBuilderArray<BatchMeshID> blobBuilderArray =
                blobBuilder.Allocate(ref animationData.batchMeshIdBlobArray, authoring.soldierWalk.meshArray.Length);

            for (int i = 0; i < authoring.soldierWalk.meshArray.Length; i++)
            {
                Mesh mesh = authoring.soldierWalk.meshArray[i];
                blobBuilderArray[i] =
                    egs.RegisterMesh(mesh);
            }

            //Build blobAssetReference
            animationDataHolder.soldierWalk = blobBuilder.CreateBlobAssetReference<AnimationData>(Allocator.Persistent);

            //Dispose resources to avoid memory leaks
            blobBuilder.Dispose();
            //Register one usage to avoid deallocation
            AddBlobAsset(ref animationDataHolder.soldierWalk, out Unity.Entities.Hash128 objectHash);
        }

        AddComponent(entity, animationDataHolder);
    }
}

public struct AnimationDataHolder : IComponentData
{
    public BlobAssetReference<AnimationData> soldierIdle;
    public BlobAssetReference<AnimationData> soldierWalk;
}

public struct AnimationData
{
    public int frameCount;
    public float frameFrequency;
    public BlobArray<BatchMeshID> batchMeshIdBlobArray;
}