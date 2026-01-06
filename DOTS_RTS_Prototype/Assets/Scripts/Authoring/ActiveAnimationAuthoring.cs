using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

class ActiveAnimationAuthoring : MonoBehaviour
{
    
}

class ActiveAnimationBaker : Baker<ActiveAnimationAuthoring>
{
    public override void Bake(ActiveAnimationAuthoring authoring)
    {
        EntitiesGraphicsSystem egs =
            World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<EntitiesGraphicsSystem>();

        Entity entity = GetEntity(TransformUsageFlags.Dynamic);        
        AddComponent(entity, new ActiveAnimation
        {
            
        });
    }
}

public struct ActiveAnimation : IComponentData
{
    public int currentFrame;
    public float framePhaseTime;
    public BlobAssetReference<AnimationData> animationDataBlobAssetReference;
}
