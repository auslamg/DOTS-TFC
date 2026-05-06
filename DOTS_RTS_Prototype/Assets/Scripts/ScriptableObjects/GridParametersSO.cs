using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "GridParametersSO", menuName = "Misc/GridParametersSO")]
public class GridParametersSO : ScriptableObject
{
    /// <summary>
    /// Grid width in cells.
    /// </summary>
    [SerializeField]
    [Tooltip("Grid width and height in cells.")]
    public int size = 16;
    /// <summary>
    /// Size of a single grid cell side in world units.
    /// </summary>
    [SerializeField]
    [Tooltip("Size of a single grid cell side in world units.")]
    public float gridCellSize = 5;

    /// <summary>
    /// Size of a single grid cell side in world units. Never used in external code.
    /// </summary>
    [SerializeField]
    [Tooltip("Utility control for validation. Never used in external code.")]
    private bool validate = false;

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
    /// Checks whether a number is a power of two.
    /// A power of two has exactly one bit set in binary form.
    ///     8 = 1000, 4 = 0100, 2 = 0010
    /// </summary>
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
    /// Returns the smallest power of two that is >= value.
    ///     5 => 8, 9 => 16, 16 => 16
    /// </summary>
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
