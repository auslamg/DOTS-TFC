using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

class ActiveAnimationAuthoring : MonoBehaviour
{
    public AnimationDataSO soldierIdle;

}

class ActiveAnimationAuthoringBaker : Baker<ActiveAnimationAuthoring>
{
    public override void Bake(ActiveAnimationAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        EntitiesGraphicsSystem egs =
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EntitiesGraphicsSystem>();

        //IDEA
        //Bake managed Meshes into BatchMeshID's
        /* BatchMeshID[] framesArrayBaked = new BatchMeshID[authoring.soldierIdle.meshArray.Length];
        for (int i = 0; i < authoring.soldierIdle.meshArray.Length; i++)
        {
            framesArrayBaked[i] = egs.RegisterMesh(authoring.soldierIdle.meshArray[i]);
        } */
        //TODO: GPT
        /* var blob = BlobAssetReference<BlobArray<BatchMeshID>>.Create(framesArrayBaked); */
        
        AddComponent(entity, new ActiveAnimation
        {
            /* framesArray = blob, */
            frame0 = egs.RegisterMesh(authoring.soldierIdle.meshArray[0]),
            frame1 = egs.RegisterMesh(authoring.soldierIdle.meshArray[1]),
            frameCount = authoring.soldierIdle.meshArray.Length,
            frameFrequency = authoring.soldierIdle.frameFrequency,
        });
    }
}

public struct ActiveAnimation : IComponentData
{
    /* public BlobAssetReference<BlobArray<BatchMeshID>> framesArray; */
    public BatchMeshID frame0;
    public BatchMeshID frame1;
    public int currentFrame;
    public int frameCount;
    public float framePhaseTime;
    public float frameFrequency;
}
