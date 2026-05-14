using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Scriptable Object defining grid configuration parameters used for grid-based systems.
/// </summary>
/// <remarks>
/// Provides grid size and cell dimensions, with validation enforcing power-of-two sizing for compatibility
/// with systems such as spatial partitioning, pathfinding, or texture-aligned grids.
/// </remarks>
[CreateAssetMenu(fileName = "GridParametersSO", menuName = "Scriptable Objects/Misc/GridParametersSO")]
public class GridParametersSO : ScriptableObject
{
    /// <summary>
    /// Grid width in cells (grid is assumed to be square).
    /// </summary>
    [SerializeField]
    [Tooltip("Grid width and height in cells.")]
    public int size = 16;

    /// <summary>
    /// Size of a single grid cell in world units.
    /// </summary>
    [SerializeField]
    [Tooltip("Size of a single grid cell side in world units.")]
    public float gridCellSize = 5;

    /// <summary>
    /// Internal validation toggle used to prevent repeated automatic correction during editing.
    /// </summary>
    [SerializeField]
    [Tooltip("Utility control for validation. Never used in external code.")]
    private bool validate = false;

    /// <summary>
    /// Unity validation callback invoked when values are modified in the inspector.
    /// Ensures grid size remains valid and adjusts it to the nearest power of two if necessary.
    /// </summary>
    private void OnValidate()
    {
        // Countermeasure for validation while still typing
        if (!validate)
            return;

        if (size < 1)
            size = 1;

        if (!IsPowerOfTwo(size))
        {
            size = NextPowerOfTwo(size);
            Debug.LogWarning($"Grid size must be a power of 2. Adjusted to {size}.", this);
        }

        validate = false;
    }

    /// <summary>
    /// Determines whether a given integer is a power of two.
    /// </summary>
    /// <param name="value">Value to evaluate.</param>
    /// <returns><see langword="true"/> if the value is a power of two; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// Uses a bitwise check based on the property that powers of two have exactly one set bit.
    /// </remarks>
    private static bool IsPowerOfTwo(int value)
    {
        // If value is 0, it's a power of two.
        // This trick works because powers of two have a special binary pattern:
        //      8  = 1000
        //      7  = 0111
        // If we AND them, result is 0.

        return (value & (value - 1)) == 0;
    }

    /// <summary>
    /// Returns the smallest power of two greater than or equal to the specified value.
    /// </summary>
    /// <param name="value">Input value to round up.</param>
    /// <returns>Next power of two greater than or equal to <paramref name="value"/>.</returns>
    /// <remarks>
    /// Example: 5 → 8, 9 → 16, 16 → 16.
    /// Uses bitwise propagation to efficiently compute the result.
    /// </remarks>
    private static int NextPowerOfTwo(int value)
    {
        // Anything less than 1 becomes 1 (smallest power of two)
        if (value < 1) return 1;

        // We subtract 1 first so exact powers of two don't jump to the next one
        value--;

        // These steps "spread" the highest set bit to all lower bits
        // using bit shifting (very fast binary operations)

        value |= value >> 1;   // copy bits right by 1
        value |= value >> 2;   // copy bits right by 2
        value |= value >> 4;   // copy bits right by 4
        value |= value >> 8;   // copy bits right by 8
        value |= value >> 16;  // copy bits right by 16

        // Now all bits below the highest bit are 1s
        // Example: 00001101 → becomes 00001111

        value++; // add 1 → becomes next power of two

        return value;
    }
}