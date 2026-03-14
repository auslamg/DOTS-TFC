using UnityEngine;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Profiling;
using System.Collections.Generic;

public class MobilePerformanceMonitor : MonoBehaviour
{
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

        PrintDeviceSpecs();
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

        GUILayout.BeginArea(new Rect(20, 20, 400, 400));

        GUILayout.Label($"FPS: {fps:F1}");
        GUILayout.Label($"Avg FPS (10s): {avgFps:F1}");

        GUILayout.Space(10);

        GUILayout.Label($"Job Threads: {activeThreads}/{maxWorkerThreads}");

        GUILayout.Space(10);

        GUILayout.Label($"CPU: {SystemInfo.processorType}");
        GUILayout.Label($"CPU Cores: {SystemInfo.processorCount}");

        GUILayout.Space(5);

        GUILayout.Label($"GPU: {SystemInfo.graphicsDeviceName}");
        GUILayout.Label($"GPU Memory: {SystemInfo.graphicsMemorySize} MB");

        GUILayout.Space(5);

        GUILayout.Label($"GPU Frame Time: {gpuFrameTime:F2} ms");

        GUILayout.Space(10);

        GUILayout.Label($"Device: {SystemInfo.deviceModel}");

        GUILayout.EndArea();
    }

    void PrintDeviceSpecs()
    {
        Debug.Log("=== DEVICE SPECS ===");
        Debug.Log($"Device Model: {SystemInfo.deviceModel}");
        Debug.Log($"CPU: {SystemInfo.processorType}");
        Debug.Log($"CPU Cores: {SystemInfo.processorCount}");
        Debug.Log($"GPU: {SystemInfo.graphicsDeviceName}");
        Debug.Log($"GPU VRAM: {SystemInfo.graphicsMemorySize} MB");
        Debug.Log($"Worker Threads: {maxWorkerThreads}");
    }

    void OnDisable()
    {
        jobThreadRecorder.Dispose();
    }
}