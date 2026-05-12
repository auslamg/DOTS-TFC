using UnityEngine;

public class TimeControlDebug : MonoBehaviour
{
    [Header("Multipliers")]

    /// <summary>
    /// .
    /// </summary>
    [SerializeField]
    [Tooltip(".")]
    public float speedUpMultiplier = 2;
    public float speedDownMultiplier = 0.5f;

    // Update is called once per frame
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
