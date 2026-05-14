using System;
using Unity.Mathematics;
using UnityEngine;
using static GridUtil;

/// <summary>
/// Visual representation of a grid chunk used for higher-level debugging visualization.
/// </summary>
public class GridChunkGizmo : MonoBehaviour
{
    /// <summary>
    /// Chunk X coordinate.
    /// </summary>
    private int x;

    /// <summary>
    /// Chunk Y coordinate.
    /// </summary>
    private int y;

    /// <summary>
    /// Unused runtime size cache.
    /// </summary>
    private int size;

    /// <summary>
    /// Unused runtime data cache.
    /// </summary>
    private byte data;

    /// <summary>
    /// Visual transform for the chunk sprite.
    /// </summary>
    [SerializeField] private Transform visual;

    /// <summary>
    /// Initializes the chunk gizmo.
    /// </summary>
    /// <param name="x">Chunk X.</param>
    /// <param name="y">Chunk Y.</param>
    /// <param name="cellSize">Cell size.</param>
    /// <param name="chunkSize">Chunk size in cells.</param>
    public void Initialize(int x, int y, float cellSize, float chunkSize)
    {
        this.x = x;
        this.y = y;

        visual = transform.GetChild(0);

        transform.position = CoordsToWorldPositionCorner(x, y, cellSize * chunkSize);
        visual.transform.position += new Vector3(-cellSize / 2, 0.2f, -cellSize / 2);

        visual.GetComponent<SpriteRenderer>().size = Vector2.one * (cellSize * chunkSize);
    }

    /// <summary>
    /// Sets chunk color.
    /// </summary>
    public void SetColor(Color color)
    {
        visual.GetComponent<SpriteRenderer>().color = color;
    }

    /// <summary>
    /// Sets chunk sprite.
    /// </summary>
    public void SetSprite(Sprite sprite)
    {
        visual.GetComponent<SpriteRenderer>().sprite = sprite;
    }

    /// <summary>
    /// Sets chunk sprite rotation.
    /// </summary>
    public void SetSpriteRotation(Quaternion rotation)
    {
        var srTransform = visual.GetComponent<SpriteRenderer>().transform;
        srTransform.rotation = rotation;
        srTransform.rotation *= Quaternion.Euler(90, 0, 0);
    }
}