using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// Managed component for the <c>AnimationDataHolder</c> unmanaged component.
/// </summary>
/// <remarks>
/// The component is a Singleton.
/// </remarks>
class AnimationDataHolderAuthoring : MonoBehaviour
{
    public AnimationDataRegistrySO animationDataRegistrySO;

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
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AnimationDataHolder animationDataHolder = new AnimationDataHolder();

        //Retrieve and allocate all AnimationDataSO into arrays and initialize inside managed AnimationDataHolder
        animationDataHolder = AllocateAnimationData(authoring);
        //Register one usage to avoid deallocation
        AddBlobAsset(ref animationDataHolder.animationDataBlobArrayAssetReference, out Unity.Entities.Hash128 objectHash);

        AddComponent(entity, animationDataHolder);
    }

    //TODO: Document
    private static AnimationDataHolder AllocateAnimationData(AnimationDataHolderAuthoring authoring)
    {
        AnimationDataHolder animationDataHolder;

        //Cache EGS to register meshes in order to bake them
        EntitiesGraphicsSystem egs =
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EntitiesGraphicsSystem>();

        //Build new blob root
        BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);
        ref BlobArray<AnimationData> animationDataBlobArray = ref blobBuilder.ConstructRoot<BlobArray<AnimationData>>();

        //Allocate memory for AnimationData array
        int blobArraySize = authoring.animationDataRegistrySO.animationDataSOList.Count();
        BlobBuilderArray<AnimationData> animationDataBlobBuilderArray =
            blobBuilder.Allocate<AnimationData>(ref animationDataBlobArray, blobArraySize);

        //For all Animation ScriptableObjects found in the list reader
        int animationSOIndex = 0;
        foreach (AnimationDataSO animationDataSO in authoring.animationDataRegistrySO.animationDataSOList)
        {
            //Allocate memory for the mesh array
            BlobBuilderArray<BatchMeshID> blobBuilderArray =
                blobBuilder.Allocate(ref animationDataBlobBuilderArray[animationSOIndex].batchMeshIdBlobArray, animationDataSO.meshArray.Length);

            //Edit singular data inside blob
            animationDataBlobBuilderArray[animationSOIndex].animationKey = animationDataSO.animationKey;
            animationDataBlobBuilderArray[animationSOIndex].playFull = animationDataSO.playFull;
            animationDataBlobBuilderArray[animationSOIndex].frameFrequency = animationDataSO.frameFrequency;
            animationDataBlobBuilderArray[animationSOIndex].frameCount = animationDataSO.meshArray.Length;

            //Register all meshes in the mesh array
            for (int i = 0; i < animationDataSO.meshArray.Length; i++)
            {
                Mesh mesh = animationDataSO.meshArray[i];
                //Add to BlobBuilder registered (baked) meshes
                blobBuilderArray[i] =
                    egs.RegisterMesh(mesh);
            }

            animationSOIndex++;
        }

        //Build blobAssetReference
        animationDataHolder.animationDataBlobArrayAssetReference =
            blobBuilder.CreateBlobAssetReference<BlobArray<AnimationData>>(Allocator.Persistent);

        //Dispose resources to avoid memory leaks
        blobBuilder.Dispose();

        return animationDataHolder;
    }
}

public struct AnimationDataHolder : IComponentData
{
    //TODO: Document
    public BlobAssetReference<BlobArray<AnimationData>> animationDataBlobArrayAssetReference;
}

public struct AnimationData
{
    public AnimationKey animationKey;
    public bool playFull;
    /// <summary>
    /// Total number of frames in the animation.
    /// </summary>
    public int frameCount;
    /// <summary>
    /// Time span for each frame change.
    /// Animations are meant to follow a strictly linear frame-rate dictated by this value.
    /// </summary>
    public float frameFrequency;
    public BlobArray<BatchMeshID> batchMeshIdBlobArray;
}