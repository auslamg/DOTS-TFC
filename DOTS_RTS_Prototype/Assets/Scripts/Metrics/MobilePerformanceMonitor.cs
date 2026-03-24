using UnityEngine;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Profiling;
using System.Collections.Generic;

public class MobilePerformanceMonitor : MonoBehaviour
{
    [SerializeField] int fontSize = 24;

    // FPS tracking
    float fps;
    float avgFps;

    const float AvgWindow = 10f;
    Queue<float> frameTimes = new Queue<float>();
    float frameTimeSum = 0f;

    // Job system
    int maxWorkerThreads;
    ProfilerRecorder jobThreadRecorder;

    // GPU timing
    FrameTiming[] frameTimings = new FrameTiming[1];
    double gpuFrameTime;

    void Start()
    {
        maxWorkerThreads = JobsUtility.JobWorkerCount;

        // Track active job threads
        jobThreadRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Internal,
            "Job System Active Threads"
        );

    }

    void Update()
    {
        float dt = Time.deltaTime;
        fps = 1f / dt;

        // ---- Rolling 10 second average ----
        frameTimes.Enqueue(dt);
        frameTimeSum += dt;

        while (frameTimeSum > AvgWindow)
        {
            frameTimeSum -= frameTimes.Dequeue();
        }

        avgFps = frameTimes.Count / frameTimeSum;

        // ---- GPU Frame Timing ----
        FrameTimingManager.CaptureFrameTimings();
        uint count = FrameTimingManager.GetLatestTimings(1, frameTimings);

        if (count > 0)
            gpuFrameTime = frameTimings[0].gpuFrameTime;
    }

    void OnGUI()
    {
        int activeThreads = jobThreadRecorder.Valid
            ? (int)jobThreadRecorder.LastValue
            : 0;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;

        GUILayout.BeginArea(new Rect(20, 20, 800, 800));

        GUILayout.Label($"FPS: {fps:F1}", style);
        GUILayout.Label($"Avg FPS (10s): {avgFps:F1}", style);

        GUILayout.Space(10);

        GUILayout.Label($"Job Threads: {activeThreads}/{maxWorkerThreads}", style);

        GUILayout.Space(10);

        GUILayout.Label($"CPU: {SystemInfo.processorType}", style);
        GUILayout.Label($"CPU Cores: {SystemInfo.processorCount}", style);

        GUILayout.Space(5);

        GUILayout.Label($"GPU: {SystemInfo.graphicsDeviceName}", style);
        GUILayout.Label($"GPU Memory: {SystemInfo.graphicsMemorySize} MB", style);

        GUILayout.Space(5);

        GUILayout.Label($"GPU Frame Time: {gpuFrameTime:F2} ms", style);

        GUILayout.Space(10);

        GUILayout.Label($"Device: {SystemInfo.deviceModel}", style);

        GUILayout.EndArea();
    }

    void OnDisable()
    {
        jobThreadRecorder.Dispose();
    }
}