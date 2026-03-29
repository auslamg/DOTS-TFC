using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor window that bakes a skinned mesh animation into per-frame Mesh assets.
/// This is used for GPU instancing in DOTS-friendly playback where a SkinnedMeshRenderer
/// is not available at runtime. The tool samples a single AnimationClip at a fixed FPS and
/// saves each baked Mesh as an asset on disk. User must pick useful frames manually to
/// ensure animation readability.
/// Supports SkinnedMeshRenderers with multiple materials.
/// </summary>
public class SkinnedMeshBaker : EditorWindow
{
    private static readonly GUIContent SourceObjectLabel = new GUIContent("Source Object", "Root GameObject containing the SkinnedMeshRenderer to bake.");
    private static readonly GUIContent ClipLabel = new GUIContent("Animation Clip", "AnimationClip sampled during baking.");
    private static readonly GUIContent OutputNameLabel = new GUIContent("Output Name", "Base name used for generated mesh assets and prefabs.");
    private static readonly GUIContent FpsLabel = new GUIContent("FPS", "Sampling rate used to bake the animation. Higher values generate more frames.");
    private static readonly GUIContent OutputPathLabel = new GUIContent("Output Path", "Project-relative Assets folder where baked meshes and prefabs are saved.");

    /// <summary>
    /// Root GameObject that contains the SkinnedMeshRenderer to bake.
    /// </summary>
    [SerializeField]
    [Tooltip("Root GameObject containing the SkinnedMeshRenderer to bake.")]
    GameObject sourceObject;

    /// <summary>
    /// AnimationClip to sample when baking.
    /// </summary>
    [SerializeField]
    [Tooltip("Animation clip sampled during baking.")]
    AnimationClip clip;

    /// <summary>
    /// Base name for the generated Mesh assets.
    /// </summary>
    [SerializeField]
    [Tooltip("Base name used for generated mesh assets and prefabs.")]
    string outputName;

    /// <summary>
    /// Sampling rate for the bake process.
    /// Higher values increase fidelity but also produce more assets.
    /// </summary>
    [SerializeField]
    [Tooltip("Sampling rate used to bake the animation. Higher values generate more frames.")]
    int framesPerSecond = 10;

    /// <summary>
    /// Folder under the Assets directory where baked meshes are stored.
    /// </summary>
    [SerializeField]
    [Tooltip("Project-relative Assets folder where baked meshes and prefabs are saved.")]
    string outputPath = "Assets/BakedMeshes";

    /// <summary>
    /// Adds a menu item that opens the bake window.
    /// </summary>
    [MenuItem("Tools/DOTS/Bake Skinned Mesh Animation")]
    static void Open()
    {
        GetWindow<SkinnedMeshBaker>("Mesh Baker");
    }

    /// <summary>
    /// Draws the editor UI and handles user input.
    /// </summary>
    void OnGUI()
    {
        // Inputs for source, clip, and output configuration.
        sourceObject = (GameObject)EditorGUILayout.ObjectField(SourceObjectLabel, sourceObject, typeof(GameObject), true);
        clip = (AnimationClip)EditorGUILayout.ObjectField(ClipLabel, clip, typeof(AnimationClip), false);
        outputName = EditorGUILayout.TextField(OutputNameLabel, outputName);
        framesPerSecond = EditorGUILayout.IntField(FpsLabel, framesPerSecond);
        outputPath = EditorGUILayout.TextField(OutputPathLabel, outputPath);

        // Disable the bake button unless we have the minimal required inputs.
        GUI.enabled = sourceObject && clip;
        if (GUILayout.Button("Bake Animation"))
        {
            Bake();
        }
        GUI.enabled = true;
    }

    /// <summary>
    /// Samples the animation clip and writes each baked mesh and prefab to the output path.
    /// Supports multiple materials.
    /// </summary>
    void Bake()
    {
        // Locate the first SkinnedMeshRenderer within the source hierarchy.
        SkinnedMeshRenderer skinnedMeshRenderer = sourceObject.GetComponentInChildren<SkinnedMeshRenderer>();
        if (!skinnedMeshRenderer)
        {
            Debug.LogError("No SkinnedMeshRenderer found");
            return;
        }

        // Prepare the folder for prefabs.
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        // Prepare the folder for meshes inside outputPath/Meshes
        string meshFolder = Path.Combine(outputPath, "Meshes");
        if (!Directory.Exists(meshFolder))
        {
            Directory.CreateDirectory(meshFolder);
        }

        // Determine how many frames to bake based on clip length and FPS.
        float clipLength = clip.length;
        int frameCount = Mathf.CeilToInt(clipLength * framesPerSecond);

        // Cache the materials from the original renderer so baked prefabs use the same materials.
        Material[] sourceMaterials = skinnedMeshRenderer.sharedMaterials;

        // Enter animation sampling mode for deterministic clip evaluation.
        AnimationMode.StartAnimationMode();

        for (int i = 0; i < frameCount; i++)
        {
            // Compute the current sample time in seconds.
            float time = i / (float)framesPerSecond;

            // Pose the source object at the target time.
            AnimationMode.SampleAnimationClip(sourceObject, clip, time);

            // Bake the current pose into a new Mesh asset.
            Mesh bakedMesh = new Mesh();
            skinnedMeshRenderer.BakeMesh(bakedMesh);

            // Ensure the baked mesh preserves all submeshes for multi-material support.
            bakedMesh.subMeshCount = sourceMaterials.Length;

            // Give the mesh a deterministic per-frame name.
            bakedMesh.name = $"{outputName}_{i}";

            // Create a new asset for the baked mesh inside the Meshes folder.
            string meshAssetPath = Path.Combine(meshFolder, bakedMesh.name + ".asset");
            AssetDatabase.CreateAsset(bakedMesh, meshAssetPath);

            // Create a GameObject that references the baked mesh and original materials.
            // This allows the baked frame to render correctly without manually assigning materials.
            GameObject frameObject = new GameObject($"{outputName}_{i}");

            // Add standard mesh components used by non-skinned meshes.
            MeshFilter meshFilter = frameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = frameObject.AddComponent<MeshRenderer>();

            // Assign the baked mesh and the original materials.
            meshFilter.sharedMesh = bakedMesh;
            meshRenderer.sharedMaterials = sourceMaterials;

            // Save the GameObject as a prefab in the main outputPath.
            string prefabPath = Path.Combine(outputPath, frameObject.name + ".prefab");
            PrefabUtility.SaveAsPrefabAsset(frameObject, prefabPath);

            // Destroy the temporary GameObject used to create the prefab.
            DestroyImmediate(frameObject);
        }

        // Exit animation sampling mode and persist assets.
        AnimationMode.StopAnimationMode();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Baked {frameCount} meshes into '{meshFolder}' and prefabs into '{outputPath}'");
    }
}