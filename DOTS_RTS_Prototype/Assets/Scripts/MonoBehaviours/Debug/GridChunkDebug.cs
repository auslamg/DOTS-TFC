using System;
using Unity.Mathematics;
using UnityEngine;
using static GridUtil;

public class GridChunkDebug : MonoBehaviour
{
    private int x;
    private int y;
    private int size;
    private byte data;

    [SerializeField] private Transform visual;

    public void Initialize(int x, int y, float cellSize, float chunkSize)
    {
        // Data
        this.x = x;
        this.y = y;
        visual = gameObject.transform.GetChild(0);

        //Adjust world position based on cell size
        transform.position = CoordsToWorldPositionCorner(x, y, cellSize * chunkSize);
        visual.transform.position = visual.transform.position + new Vector3(-cellSize / 2, 0.2f, -cellSize / 2);

        //Adjust visual scale based on cell size
        /* visual.localScale = new Vector3(cellSize * chunkSize, cellSize * chunkSize, cellSize * chunkSize); */
        visual.gameObject.GetComponent<SpriteRenderer>().size = Vector2.one * (cellSize * chunkSize);
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
