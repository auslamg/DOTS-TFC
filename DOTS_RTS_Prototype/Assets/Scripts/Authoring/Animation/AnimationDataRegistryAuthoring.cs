using System;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
/// <summary>
/// Authoring component that bakes data into <see cref="AnimationDataRegistry"/>.
/// </summary>
/// <remarks>
/// Behaves as a scene singleton.
/// </remarks>
class AnimationDataRegistryAuthoring : MonoBehaviour
{
    /// <summary>
    /// Source scriptable object containing all animation definitions and frame meshes.
    /// </summary>
    [SerializeField]
    [Tooltip("Scriptable object containing all animation definitions and frame meshes.")]
    public AnimationDataRegistrySO animationDataRegistrySO;
    /// <summary>
    /// Default material used to register baked animation frame meshes.
    /// </summary>
    [SerializeField]
    [Tooltip("Default material used when registering baked animation frame meshes.")]
    public Material defaultMaterial; //Used exclusively to avoid submesh with no material warning (unity bug since there is no submesh)
    /// <summary>
    /// Optional secondary material used when a mesh has multiple submeshes.
    /// </summary>
    [SerializeField]
    [Tooltip("Optional second material used when an animation mesh has multiple submeshes.")]
    public Material secondMaterial; //Used when mesh has two materials

    /// <summary>
    /// Scene singleton instance for managed-side access.
    /// </summary>
    public static AnimationDataRegistryAuthoring Instance { get; private set; }

    /// <summary>
    /// Initializes singleton instance state.
    /// </summary>
    void Awake()
    {
        // Initialize singleton instance state.
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
            /* Debug.Log($"Baking animation data: {animationDataSO.name}"); */

            //Register all meshes in the mesh array
            for (int i = 0; i < animationDataSO.meshArray.Length; i++)
            {
                //Obtain each mesh from animationDataSO
                Mesh mesh = animationDataSO.meshArray[i];

                // If mesh has 2+ submeshes, create entities for each material
                if (mesh.subMeshCount >= 2 && authoring.secondMaterial != null)
                {
                    // Create entity with default material
                    {
                        Entity meshBakingEntity = CreateAdditionalEntity(TransformUsageFlags.None, true);
                        AddComponent(meshBakingEntity, new MaterialMeshInfo());
                        AddComponent(meshBakingEntity, new RenderMeshUnmanaged
                        {
                            materialForSubMesh = authoring.defaultMaterial,
                            mesh = mesh,
                        });
                        AddComponent(meshBakingEntity, new AnimationFrameMetadata
                        {
                            animationKey = animationDataSO.animationKey,
                            meshIndex = i,
                        });
                    }

                    // Create entity with second material
                    {
                        Entity meshBakingEntity = CreateAdditionalEntity(TransformUsageFlags.None, true);
                        AddComponent(meshBakingEntity, new MaterialMeshInfo());
                        AddComponent(meshBakingEntity, new RenderMeshUnmanaged
                        {
                            materialForSubMesh = authoring.secondMaterial,
                            mesh = mesh,
                        });
                        AddComponent(meshBakingEntity, new AnimationFrameMetadata
                        {
                            animationKey = animationDataSO.animationKey,
                            meshIndex = i,
                        });
                    }
                }
                else
                {
                    // Single material case
                    Entity meshBakingEntity = CreateAdditionalEntity(TransformUsageFlags.None, true);
                    AddComponent(meshBakingEntity, new MaterialMeshInfo());
                    AddComponent(meshBakingEntity, new RenderMeshUnmanaged
                    {
                        materialForSubMesh = authoring.defaultMaterial,
                        mesh = mesh,
                    });
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
    /// <summary>
    /// Animation key that owns this frame mesh entry.
    /// </summary>
    public AnimationKey animationKey;
    /// <summary>
    /// Frame index within the animation mesh array.
    /// </summary>
    public int meshIndex;
}

/// <summary>
/// Used exclusively for passing down a managed <see cref="AnimationDataRegistrySO"/> reference to the PostBaking process.
/// See <see cref="AnimationDataRegistryPostBakingSystem"/>
/// </summary>
public struct AnimationRegistrySOReference : IComponentData
{
    /// <summary>
    /// Managed reference to the source animation registry SO for post-baking processing.
    /// </summary>
    public UnityObjectRef<AnimationDataRegistrySO> registry;
}

/// <summary>
/// Singleton component containing animation data baked from <see cref="AnimationDataRegistrySO"/> during post-baking.
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
    /// <summary>
    /// Unique key for this animation entry.
    /// </summary>
    public AnimationKey animationKey;
    /// <summary>
    /// High-level animation category.
    /// </summary>
    public AnimationType animationType;
    /// <summary>
    /// Whether this animation must finish before being interrupted.
    /// </summary>
    [MarshalAs(UnmanagedType.U1)]
    public bool playFull;
    /// <summary>
    /// Time interval for each frame change.
    /// Animations are meant to follow a strictly linear frame-rate dictated by this value.
    /// </summary>
    public float frameFrequency;
    /// <summary>
    /// Total number of frames in the animation.
    /// </summary>
    public int frameCount;
    /// <summary>
    /// Blob array of each animation frame mesh ID.
    /// </summary>
    public BlobArray<int> frameMeshIdIndex;

    /// <summary>
    /// Returns whether this animation should be treated as uninterruptible.
    /// </summary>
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