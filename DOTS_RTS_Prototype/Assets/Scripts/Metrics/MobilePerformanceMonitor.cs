using UnityEngine;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Profiling;
using System.Collections.Generic;

/// <summary>
/// Renders an on-screen performance overlay displaying FPS, averaged FPS over time,
/// Unity job system thread usage, and GPU frame time information.
/// </summary>
public class MobilePerformanceMonitor : MonoBehaviour
{
    /// <summary>
    /// Font size used for all on-screen performance labels.
    /// </summary>
    [Tooltip("Font size used for all on-screen performance labels.")]
    [SerializeField] int fontSize = 24;

    /// <summary>
    /// Time interval (in seconds) used to sample instantaneous FPS.
    /// </summary>
    [Tooltip("Time window (in seconds) used to sample FPS.")]
    [SerializeField] float fpsSampleInterval = 0.1f;

    /// <summary>
    /// Current sampled FPS value.
    /// </summary>
    float fps;

    /// <summary>
    /// Rolling average FPS over a fixed time window.
    /// </summary>
    float avgFps;

    /// <summary>
    /// Time window used for rolling average FPS calculation.
    /// </summary>
    const float AvgWindow = 10f;

    /// <summary>
    /// Queue storing frame delta times for rolling average calculation.
    /// </summary>
    Queue<float> frameTimes = new Queue<float>();

    /// <summary>
    /// Accumulated frame time sum used for rolling average computation.
    /// </summary>
    float frameTimeSum = 0f;

    /// <summary>
    /// Timer used to accumulate FPS sampling interval.
    /// </summary>
    float fpsTimer = 0f;

    /// <summary>
    /// Number of frames counted within the current FPS sampling window.
    /// </summary>
    int fpsFrameCount = 0;

    /// <summary>
    /// Maximum number of worker threads available to Unity Job System.
    /// </summary>
    int maxWorkerThreads;

    /// <summary>
    /// Profiler recorder used to track active Unity Job System threads.
    /// </summary>
    ProfilerRecorder jobThreadRecorder;

    /// <summary>
    /// Buffer used to retrieve GPU frame timing data.
    /// </summary>
    FrameTiming[] frameTimings = new FrameTiming[1];

    /// <summary>
    /// Last recorded GPU frame time in milliseconds.
    /// </summary>
    double gpuFrameTime;

    /// <summary>
    /// Initializes job system thread tracking and profiler recorder.
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
    /// Updates FPS metrics, rolling averages, and GPU timing data each frame.
    /// </summary>
    void Update()
    {
        float dt = Time.deltaTime;

        // --- Configurable FPS sampling ---
        fpsTimer += dt;
        fpsFrameCount++;

        if (fpsTimer >= fpsSampleInterval)
        {
            fps = fpsFrameCount / fpsTimer;
            fpsTimer = 0f;
            fpsFrameCount = 0;
        }

        // --- Rolling average over the last AvgWindow seconds ---
        frameTimes.Enqueue(dt);
        frameTimeSum += dt;

        while (frameTimeSum > AvgWindow)
        {
            frameTimeSum -= frameTimes.Dequeue();
        }

        avgFps = frameTimes.Count / frameTimeSum;

        // --- GPU frame timing ---
        FrameTimingManager.CaptureFrameTimings();
        uint count = FrameTimingManager.GetLatestTimings(1, frameTimings);

        if (count > 0)
            gpuFrameTime = frameTimings[0].gpuFrameTime;
    }

    /// <summary>
    /// Renders the performance overlay using Unity IMGUI.
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
    /// Releases profiler resources when the component is disabled.
    /// </summary>
    void OnDisable()
    {
        jobThreadRecorder.Dispose();
    }
}