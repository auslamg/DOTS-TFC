using UnityEngine;

/// <summary>
/// Resets the <see cref="RectTransform"/> anchored position and size delta to zero on startup, then removes itself.
/// </summary>
/// <remarks>
/// Attach to UI elements that need their layout overrides cleared at runtime without leaving a persistent component.
/// </remarks>
public class ResetUIPosition : MonoBehaviour
{
    /// <summary>
    /// Resets anchored position and size delta, then destroys this component.
    /// </summary>
    private void Awake()
    {
        RectTransform rt = GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        Destroy(this);
    }
}
