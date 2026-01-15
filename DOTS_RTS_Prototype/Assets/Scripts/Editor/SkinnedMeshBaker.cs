using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class SkinnedMeshBaker : EditorWindow
{
    [SerializeField] GameObject sourceObject;
    [SerializeField] AnimationClip clip;
    [SerializeField] int framesPerSecond = 10;
    [SerializeField] string outputPath = "Assets/BakedMeshes";

    [MenuItem("Tools/DOTS/Bake Skinned Mesh Animation")]
    static void Open()
    {
        GetWindow<SkinnedMeshBaker>("Mesh Baker");
    }

    void OnGUI()
    {
        sourceObject = (GameObject)EditorGUILayout.ObjectField("Source Object", sourceObject, typeof(GameObject), true);
        clip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", clip, typeof(AnimationClip), false);
        framesPerSecond = EditorGUILayout.IntField("FPS", framesPerSecond);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        GUI.enabled = sourceObject && clip;
        if (GUILayout.Button("Bake Animation"))
        {
            Bake();
        }
        GUI.enabled = true;
    }

    void Bake()
    {
        SkinnedMeshRenderer skinnedMeshRenderer = sourceObject.GetComponentInChildren<SkinnedMeshRenderer>();
        if (!skinnedMeshRenderer)
        {
            Debug.LogError("No SkinnedMeshRenderer found");
            return;
        }

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        float clipLength = clip.length;
        int frameCount = Mathf.CeilToInt(clipLength * framesPerSecond);

        AnimationMode.StartAnimationMode();

        for (int i = 0; i < frameCount; i++)
        {
            float time = i / (float)framesPerSecond;

            AnimationMode.SampleAnimationClip(sourceObject, clip, time);

            Mesh bakedMesh = new Mesh();
            skinnedMeshRenderer.BakeMesh(bakedMesh);

            bakedMesh.name = $"{clip.name}_frame_{i:D4}";

            string assetPath = $"{outputPath}/{bakedMesh.name}.asset";
            AssetDatabase.CreateAsset(bakedMesh, assetPath);
        }

        AnimationMode.StopAnimationMode();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Baked {frameCount} meshes to {outputPath}");
    }
}
