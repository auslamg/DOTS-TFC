using System;
using Unity.Mathematics;
using UnityEngine;
using static GridUtil;

/// <summary>
/// Visual representation of a single grid cell used for debugging and flow-field visualization.
/// </summary>
public class GridCellGizmo : MonoBehaviour
{
    /// <summary>
    /// Grid X coordinate.
    /// </summary>
    private int x;

    /// <summary>
    /// Grid Y coordinate.
    /// </summary>
    private int y;

    /// <summary>
    /// Unused runtime data cache.
    /// </summary>
    private byte data;

    /// <summary>
    /// Visual transform for the cell sprite.
    /// </summary>
    [SerializeField] private Transform visual;

    /// <summary>
    /// Initializes the cell gizmo at a grid coordinate.
    /// </summary>
    /// <param name="x">Grid X.</param>
    /// <param name="y">Grid Y.</param>
    /// <param name="cellSize">Size of a grid cell.</param>
    public void Initialize(int x, int y, float cellSize)
    {
        this.x = x;
        this.y = y;

        visual = transform.GetChild(0);
        transform.position = CoordsToWorldPositionCorner(x, y, cellSize);
        visual.localScale = new Vector3(cellSize, cellSize, cellSize);
    }

    /// <summary>
    /// Sets the cell sprite color.
    /// </summary>
    /// <param name="color">Tint color.</param>
    public void SetColor(Color color)
    {
        visual.GetComponent<SpriteRenderer>().color = color;
    }

    /// <summary>
    /// Sets the cell sprite.
    /// </summary>
    /// <param name="sprite">Sprite to display.</param>
    public void SetSprite(Sprite sprite)
    {
        visual.GetComponent<SpriteRenderer>().sprite = sprite;
    }

    /// <summary>
    /// Sets the cell sprite rotation.
    /// </summary>
    /// <param name="rotation">Rotation to apply.</param>
    public void SetSpriteRotation(Quaternion rotation)
    {
        var srTransform = visual.GetComponent<SpriteRenderer>().transform;
        srTransform.rotation = rotation;
        srTransform.rotation *= Quaternion.Euler(90, 0, 0);
    }
}