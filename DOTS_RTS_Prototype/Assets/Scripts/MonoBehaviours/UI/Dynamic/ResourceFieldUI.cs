using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a single resource's icon and current amount in the resource manager UI.
/// </summary>
/// <remarks>
/// This component is instantiated per resource and maintains the visual representation of a resource field,
/// including its icon image and text amount display. It is controlled by the ResourceManagerUI component.
/// </remarks>
public class ResourceFieldUI : MonoBehaviour
{
    /// <summary>
    /// Image component used to display the resource icon.
    /// </summary>
    [SerializeField]
    [Tooltip("Image component used to display the resource icon.")]
    Image icon;

    /// <summary>
    /// Text component used to display the resource amount.
    /// </summary>
    [SerializeField]
    [Tooltip("Text component used to display the resource amount.")]
    TextMeshProUGUI textMesh;

    /// <summary>
    /// Initializes the resource field with the given resource data.
    /// </summary>
    /// <param name="resourceSO">Resource definition containing icon and initial data.</param>
    public void Initialize(ResourceSO resourceSO)
    {
        icon.sprite = resourceSO.icon;
        textMesh.text = "0";
    }

    /// <summary>
    /// Updates the displayed resource amount.
    /// </summary>
    /// <param name="amount">The current resource amount to display.</param>
    public void UpdateAmount(int amount)
    {
        textMesh.text = amount.ToString();
    }
}
