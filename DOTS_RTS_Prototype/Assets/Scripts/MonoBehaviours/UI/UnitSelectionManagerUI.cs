using System;
using UnityEngine;

/// <summary>
/// Renders and updates the drag-selection rectangle used by <see cref="UnitSelectionManager"/>.
/// </summary>
/// <remarks>
/// This component listens to selection start/end events, toggles rectangle visibility,
/// and continuously updates anchored position/size while dragging.
/// </remarks>
public class UnitSelectionManagerUI : MonoBehaviour
{
    /// <summary>
    /// UI rectangle used to visualize the current selection area.
    /// </summary>
    [SerializeField]
    [Tooltip("RectTransform used to display the drag-selection rectangle.")]
    private RectTransform selectionAreaRectTransform;

    /// <summary>
    /// Canvas used to convert world-independent selection rect values into UI space.
    /// </summary>
    [SerializeField]
    [Tooltip("Canvas containing the selection rectangle, used for scale conversion.")]
    private Canvas canvas;


    /// <summary>
    /// Subscribes to selection events and initializes rectangle as hidden.
    /// </summary>
    void Start()
    {
        UnitSelectionManager.Instance.OnSelectionAreaStart += UnitSelectionManager_OnSelectionAreaStart;
        UnitSelectionManager.Instance.OnSelectionAreaEnd += UnitSelectionManager_OnSelectionAreaEnd;

        selectionAreaRectTransform.gameObject.SetActive(false);
    }

    /// <summary>
    /// Updates selection rectangle while it is visible.
    /// </summary>
    void Update()
    {
        //Updates the visual constantly when active
        if (selectionAreaRectTransform.gameObject.activeSelf)
        {
            UpdateVisual();
        }
    }

    /// <summary>
    /// Event subscriber to OnSelectionAreaStart to show and update the visual on event Invoke.
    /// </summary>
    /// <param name="sender">Unused event sender.</param>
    /// <param name="e">Unused event payload.</param>
    private void UnitSelectionManager_OnSelectionAreaStart(object sender, EventArgs e)
    {
        selectionAreaRectTransform.gameObject.SetActive(true);
        UpdateVisual();
    }

    /// <summary>
    /// Event subscriber to OnSelectionAreaEnd to hide the visual on event invoke.
    /// Updating the visual is not necessary since it is hidden.
    /// </summary>
    /// <param name="sender">Unused event sender.</param>
    /// <param name="e">Unused event payload.</param>
    private void UnitSelectionManager_OnSelectionAreaEnd(object sender, EventArgs e)
    {
        selectionAreaRectTransform.gameObject.SetActive(false);
    }

    /// <summary>
    /// Retrieves selection area rectangle from the UnitSelectionManager and updates the visual GameObject.
    /// </summary>
    private void UpdateVisual()
    {
        Rect selectionAreaRect = UnitSelectionManager.Instance.GetSelectionAreaRect();

        float canvasScale = canvas.transform.localScale.x;
        selectionAreaRectTransform.anchoredPosition = new Vector2(selectionAreaRect.x, selectionAreaRect.y) / canvasScale;
        selectionAreaRectTransform.sizeDelta = new Vector2(selectionAreaRect.width, selectionAreaRect.height) / canvasScale;
    }
}
