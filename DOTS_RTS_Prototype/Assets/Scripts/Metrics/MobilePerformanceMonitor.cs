using UnityEngine;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Profiling;
using System.Collections.Generic;

/// <summary>
/// Renders an on-screen performance overlay showing FPS, rolling average, job thread usage, and GPU frame time.
/// </summary>
public class MobilePerformanceMonitor : MonoBehaviour
{
    [Tooltip("Font size used for all on-screen performance labels.")]
    [SerializeField] int fontSize = 24;

    float fps;
    float avgFps;

    const float AvgWindow = 10f;
    Queue<float> frameTimes = new Queue<float>();
    float frameTimeSum = 0f;

    int maxWorkerThreads;
    ProfilerRecorder jobThreadRecorder;

    FrameTiming[] frameTimings = new FrameTiming[1];
    double gpuFrameTime;

    /// <summary>
    /// Caches worker thread count and starts the job thread profiler recorder.
    /// </summary>
    void Start()
    {
        maxWorkerThreads = JobsUtility.JobWorkerCount;

        jobThreadRecorder = ProfilerRecorder.StartNew(
            ProfilerCategory.Internal,
            "Job System Active Threads"
        );
    }

    /// <summary>
    /// Samples instantaneous FPS, maintains the rolling average window, and captures GPU frame timing.
    /// </summary>
    void Update()
    {
        float dt = Time.deltaTime;
        fps = 1f / dt;

        // Rolling average over the last AvgWindow seconds
        frameTimes.Enqueue(dt);
        frameTimeSum += dt;

        while (frameTimeSum > AvgWindow)
        {
            frameTimeSum -= frameTimes.Dequeue();
        }

        avgFps = frameTimes.Count / frameTimeSum;

        // GPU frame timing
        FrameTimingManager.CaptureFrameTimings();
        uint count = FrameTimingManager.GetLatestTimings(1, frameTimings);

        if (count > 0)
            gpuFrameTime = frameTimings[0].gpuFrameTime;
    }

    /// <summary>
    /// Draws the performance overlay using immediate-mode GUI.
    /// </summary>
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

    /// <summary>
    /// Releases the profiler recorder when the component is disabled.
    /// </summary>
    void OnDisable()
    {
        jobThreadRecorder.Dispose();
    }
}
