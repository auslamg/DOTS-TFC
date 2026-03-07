using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// Managed component for the <see cref="AnimationDataRegistry"/> unmanaged component.
/// </summary>
/// <remarks>
/// The component is a Singleton.
/// </remarks>
class AnimationDataRegistryAuthoring : MonoBehaviour
{
    public AnimationDataRegistrySO animationDataRegistrySO;
    public Material defaultMaterial; //Used exclusively to avoid submesh with no material warning (unity bug since there is no submesh)

    public static AnimationDataRegistryAuthoring Instance { get; private set; }

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

/// <summary>
/// Baker for the <see cref="AnimationDataRegistry"/> unmanaged component.
/// The process requires PostBaking because <see cref="EntitiesGraphicsSystem"/> isn't registered during bake-time, only after.
/// Therefore the meshes are baked in additional objects "meshBakingEntity" and later registered in PostBaking.
/// See <see cref="AnimationDataRegistryPostBakingSystem"/>
/// </summary>
class AnimationDataRegistryBaker : Baker<AnimationDataRegistryAuthoring>
{
    public override void Bake(AnimationDataRegistryAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        //For all Animation ScriptableObjects found in the list reader
        foreach (AnimationDataSO animationDataSO in authoring.animationDataRegistrySO.animationDataSOList)
        {
            //Register all meshes in the mesh array
            for (int i = 0; i < animationDataSO.meshArray.Length; i++)
            {
                //Obtain each mesh from animationDataSO
                Mesh mesh = animationDataSO.meshArray[i];

                /* Generates an entity "meshBakingEntity" per each frame to link its components to said entity and bake it, and register the entity in the aniamtionDataRegistry. */
                ///See <see cref="AnimationDataRegistryPostBakingSystem"/>
                {
                    Entity meshBakingEntity = CreateAdditionalEntity(TransformUsageFlags.None, true);
                    //Add unmanaged components needed for rendering
                    AddComponent(meshBakingEntity, new MaterialMeshInfo());
                    AddComponent(meshBakingEntity, new RenderMeshUnmanaged
                    {
                        materialForSubMesh = authoring.defaultMaterial,
                        mesh = mesh,
                    });
                    //Add animation metadata used for identification
                    AddComponent(meshBakingEntity, new AnimationFrameMetadata
                    {
                        animationKey = animationDataSO.animationKey,
                        meshIndex = i,
                    });
                }
            }
        }

        //Add reference to SO registry to access SO's during post baking.
        ///See <see cref="AnimationDataRegistryPostBakingSystem"/>
        AddComponent(entity, new AnimationRegistrySOReference
        {
            registry = authoring.animationDataRegistrySO,
        });

        AddComponent(entity, new AnimationDataRegistry());
    }
}

/// <summary>
/// Used for passing down AnimationKey and MeshIndex to the postBaking process
/// </summary>
public struct AnimationFrameMetadata : IComponentData
{
    public AnimationKey animationKey;
    public int meshIndex;
}

/// <summary>
/// Used exclusively for passing down a managed <see cref="AnimationDataRegistrySO"/> reference to the PostBaking process.
/// See <see cref="AnimationDataRegistryPostBakingSystem"/>
/// </summary>
public struct AnimationRegistrySOReference : IComponentData
{
    public UnityObjectRef<AnimationDataRegistrySO> registry;
}

/// <summary>
/// Contains the entirety of the animation data baked from the <see cref="AnimationDataRegistrySO"/> for one full animation. Built during PostBaking process.
/// </summary>
public struct AnimationDataRegistry : IComponentData
{
    /// <summary>
    /// Reference to the BlobArray containing all AnimationData (EGS mesh arrays).
    /// </summary>
    public BlobAssetReference<BlobArray<AnimationData>> animationDataBlobArrayReference;
}

/// <summary>
/// Contains the animation data baked from an <see cref="AnimationDataSO"/>.
/// </summary>
public struct AnimationData
{
    public AnimationKey animationKey;
    public AnimationType animationType;
    public bool playFull;
    /// <summary>
    /// Time span for each frame change.
    /// Animations are meant to follow a strictly linear frame-rate dictated by this value.
    /// </summary>
    public float frameFrequency;
    /// <summary>
    /// Total number of frames in the animation.
    /// </summary>
    public int frameCount;
    /// <summary>
    /// Blob array of each animation fram ID.
    /// </summary>
    public BlobArray<int> frameMeshIdIndex;

    public bool IsUninterruptible()
    {
        if (playFull) return true;
        return animationType switch
        {
            AnimationType.Melee => true,
            AnimationType.Shoot => true,
            _ => false
        };
    }
}