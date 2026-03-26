using UnityEngine;

public class ResetUIPosition : MonoBehaviour
{
    private void Awake() {
        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        GetComponent<RectTransform>().sizeDelta = Vector2.zero;

        Destroy(this);
    }
}
