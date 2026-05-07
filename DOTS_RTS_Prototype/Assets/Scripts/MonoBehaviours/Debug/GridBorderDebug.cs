using System;
using Unity.Mathematics;
using UnityEngine;
using static GridUtil;

public class GridBorderDebug : MonoBehaviour
{
    private int size;
    private byte data;

    [SerializeField] private Transform visual;

    public void Initialize(float size, float gridCellSize)
    {
        // Data
        visual = gameObject.transform.GetChild(0);

        //Adjust world position based on cell size
        transform.position = float3.zero;
        visual.transform.position = visual.transform.position + new Vector3(-gridCellSize / 2, 0.2f, -gridCellSize / 2);

        //Adjust visual scale based on cell size
        visual.gameObject.GetComponent<SpriteRenderer>().size = Vector2.one * size;
    }

    public void SetColor(Color color)
    {
        visual.gameObject.GetComponent<SpriteRenderer>().color = color;
    }

    public void SetSprite(Sprite sprite)
    {
        visual.gameObject.GetComponent<SpriteRenderer>().sprite = sprite;
    }

    public void SetSpriteRotation(Quaternion rotation)
    {
        visual.gameObject.GetComponent<SpriteRenderer>().transform.rotation = rotation;
        visual.gameObject.GetComponent<SpriteRenderer>().transform.rotation *= Quaternion.Euler(90, 0, 0);
    }
}
