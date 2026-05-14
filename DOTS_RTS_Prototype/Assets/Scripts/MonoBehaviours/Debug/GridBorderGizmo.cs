using System;
using Unity.Mathematics;
using UnityEngine;
using static GridUtil;

/// <summary>
/// Visual representation of a grid border element used for debugging and editor visualization.
/// </summary>
public class GridBorderGizmo : MonoBehaviour
{
    /// <summary>
    /// Visual transform used to render the border sprite.
    /// </summary>
    [SerializeField] private Transform visual;

    /// <summary>
    /// Initializes the border gizmo based on grid size and cell size.
    /// </summary>
    /// <param name="size">World-space size of the grid.</param>
    /// <param name="gridCellSize">Size of a single grid cell.</param>
    public void Initialize(float size, float gridCellSize)
    {
        visual = gameObject.transform.GetChild(0);

        transform.position = float3.zero;
        visual.transform.position += new Vector3(-gridCellSize / 2, 0.2f, -gridCellSize / 2);

        visual.gameObject.GetComponent<SpriteRenderer>().size = Vector2.one * size;
    }

    /// <summary>
    /// Sets the border sprite color.
    /// </summary>
    /// <param name="color">Tint color.</param>
    public void SetColor(Color color)
    {
        visual.gameObject.GetComponent<SpriteRenderer>().color = color;
    }

    /// <summary>
    /// Sets the border sprite.
    /// </summary>
    /// <param name="sprite">Sprite to display.</param>
    public void SetSprite(Sprite sprite)
    {
        visual.gameObject.GetComponent<SpriteRenderer>().sprite = sprite;
    }

    /// <summary>
    /// Sets the border sprite rotation.
    /// </summary>
    /// <param name="rotation">Desired rotation.</param>
    public void SetSpriteRotation(Quaternion rotation)
    {
        visual.gameObject.GetComponent<SpriteRenderer>().transform.rotation = rotation;
        visual.gameObject.GetComponent<SpriteRenderer>().transform.rotation *= Quaternion.Euler(90, 0, 0);
    }
}