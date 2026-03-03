using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Editor window that bakes a skinned mesh animation into per-frame Mesh assets.
/// This is used for GPU instancing in DOTS-friendly playback where a SkinnedMeshRenderer
/// is not available at runtime. The tool samples a single AnimationClip at a fixed FPS and
/// saves each baked Mesh as an asset on disk. User must pick useful frames manually to
/// ensure animation readability.
/// </summary>
public class SkinnedMeshBaker : EditorWindow
{
    /// <summary>
    /// Root GameObject that contains the SkinnedMeshRenderer to bake.
    /// </summary>
    [SerializeField] GameObject sourceObject;

    /// <summary>
    /// AnimationClip to sample when baking.
    /// </summary>
    [SerializeField] AnimationClip clip;

    /// <summary>
    /// Base name for the generated Mesh assets.
    /// </summary>
    [SerializeField] string outputName;

    /// <summary>
    /// Sampling rate for the bake process.
    /// Higher values increase fidelity but also produce more assets.
    /// </summary>
    [SerializeField] int framesPerSecond = 10;

    /// <summary>
    /// Folder under the Assets directory where baked meshes are stored.
    /// </summary>
    [SerializeField] string outputPath = "Assets/BakedMeshes";

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
        sourceObject = (GameObject)EditorGUILayout.ObjectField("Source Object", sourceObject, typeof(GameObject), true);
        clip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", clip, typeof(AnimationClip), false);
        outputName = EditorGUILayout.TextField("Output Name", outputName);
        framesPerSecond = EditorGUILayout.IntField("FPS", framesPerSecond);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        // Disable the bake button unless we have the minimal required inputs.
        GUI.enabled = sourceObject && clip;
        if (GUILayout.Button("Bake Animation"))
        {
            Bake();
        }
        GUI.enabled = true;
    }

    /// <summary>
    /// Samples the animation clip and writes each baked mesh to the output path.
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

        // Ensure the output folder exists before writing assets.
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        // Determine how many frames to bake based on clip length and FPS.
        float clipLength = clip.length;
        int frameCount = Mathf.CeilToInt(clipLength * framesPerSecond);

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

            // Give the mesh a deterministic per-frame name.
            bakedMesh.name = $"{outputName}_{i}";

            // Create a new asset for the baked mesh.
            string assetPath = $"{outputPath}/{bakedMesh.name}.asset";
            AssetDatabase.CreateAsset(bakedMesh, assetPath);
        }

        // Exit animation sampling mode and persist assets.
        AnimationMode.StopAnimationMode();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Baked {frameCount} meshes to {outputPath}");
    }
}
