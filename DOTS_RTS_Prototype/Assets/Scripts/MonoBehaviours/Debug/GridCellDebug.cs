using UnityEngine;

public class GridCellDebug : MonoBehaviour
{
    private int x;
    private int y;
    private byte data;

    [SerializeField] private Transform visual;

    public void Initialize(int x, int y, float cellSize)
    {
        // Data
        this.x = x;
        this.y = y;
        visual = gameObject.transform.GetChild(0);

        //Adjust world position based on cell size
        transform.position = GridSystem.CoordsToWorldPositionCorner(x, y, cellSize);

        //Adjust visual scale based on cell size
        visual.localScale = new Vector3(cellSize, cellSize, cellSize) * 0.5f;
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
        visual.gameObject.GetComponent<SpriteRenderer>().transform.rotation *= Quaternion.Euler(90,0,0);
    }

}
