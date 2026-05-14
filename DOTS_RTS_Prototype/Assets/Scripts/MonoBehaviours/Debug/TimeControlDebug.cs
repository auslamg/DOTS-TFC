using UnityEngine;

/// <summary>
/// Simple debug utility for controlling global game time scale at runtime.
/// </summary>
/// <remarks>
/// Allows increasing or decreasing <see cref="Time.timeScale"/> using keyboard input.
/// Intended for debugging simulation speed, animation pacing, and ECS behavior timing.
/// </remarks>
public class TimeControlDebug : MonoBehaviour
{
    [Header("Multipliers")]

    /// <summary>
    /// Multiplier applied when increasing game speed.
    /// </summary>
    [SerializeField]
    [Tooltip("Multiplier applied when increasing game speed.")]
    public float speedUpMultiplier = 2;

    /// <summary>
    /// Multiplier applied when decreasing game speed.
    /// </summary>
    [SerializeField]
    [Tooltip("Multiplier applied when decreasing game speed.")]
    public float speedDownMultiplier = 0.5f;

    /// <summary>
    /// Checks for input and adjusts global time scale accordingly.
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            Time.timeScale *= speedUpMultiplier;
        }
        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            Time.timeScale *= speedDownMultiplier;
        }
    }
}