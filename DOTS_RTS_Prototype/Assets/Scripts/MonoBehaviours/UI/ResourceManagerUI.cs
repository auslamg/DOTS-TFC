using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and maintains the resource display UI, showing current resource amounts in real-time.
/// </summary>
/// <remarks>
/// This component instantiates resource field UI elements based on the resource registry,
/// listens to resource value changes, and updates the displayed amounts accordingly.
/// It manages a dictionary of resource fields for efficient lookups and updates.
/// </remarks>
public class ResourceManagerUI : MonoBehaviour
{
    /// <summary>
    /// Parent container where resource fields are instantiated.
    /// </summary>
    [SerializeField]
    [Tooltip("Parent container where resource fields are instantiated.")]
    private RectTransform resourceFieldContainer;

    /// <summary>
    /// Template rect used to instantiate one per resource.
    /// </summary>
    [SerializeField]
    [Tooltip("Template rect used to instantiate one per resource.")]
    private RectTransform resourceFieldTemplate;

    /// <summary>
    /// Resource registry used to populate resource fields.
    /// </summary>
    [SerializeField]
    [Tooltip("Resource registry used to populate resource fields.")]
    private ResourceRegistrySO resourceRegistrySO;

    /// <summary>
    /// Fallback sprite used when a resource has no icon image configured.
    /// </summary>
    [SerializeField]
    [Tooltip("Fallback sprite used when a resource has no icon image.")]
    private Sprite placeholderResourceIconImage;

    /// <summary>
    /// Runtime cache mapping each resource definition to its instantiated UI field.
    /// </summary>
    private Dictionary<ResourceSO, ResourceFieldUI> resourceFieldDictionary;

    /// <summary>
    /// Initializes template state and builds one resource field for each resource entry.
    /// </summary>
    private void Awake()
    {
        resourceFieldTemplate.gameObject.SetActive(false);
        resourceFieldDictionary = new Dictionary<ResourceSO, ResourceFieldUI>();
        InitializeUI();
    }

    /// <summary>
    /// Rebuilds resource fields from the registry, scrapping any existing ones.
    /// </summary>
    private void InitializeUI()
    {
        ScrapResourceFields();
        ConstructResourceFields();
    }

    /// <summary>
    /// Subscribes to resource value changes after scene initialization.
    /// </summary>
    void Start()
    {
        InitializeUI_PostBake();
    }

    /// <summary>
    /// Subscribes to resource value change events from the ResourceManager.
    /// </summary>
    private void InitializeUI_PostBake()
    {
        ResourceManager.Instance.OnResourceValueChange += ResourceManager_OnResourceValueChange;
        UpdateResourceValues();
    }

    /// <summary>
    /// Handles resource value change events and updates the UI display.
    /// </summary>
    /// <param name="sender">Unused event sender.</param>
    /// <param name="e">Unused event payload.</param>
    private void ResourceManager_OnResourceValueChange(object sender, EventArgs e)
    {
        UpdateResourceValues();
    }

    /// <summary>
    /// Destroys all instantiated resource field UI elements, preserving the template.
    /// </summary>
    private void ScrapResourceFields()
    {
        foreach (Transform child in resourceFieldContainer)
        {
            if (child.gameObject == resourceFieldTemplate.gameObject)
            {
                continue;
            }
            else
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Instantiates resource fields for all non-None resource types from the registry.
    /// </summary>
    private void ConstructResourceFields()
    {
        foreach (ResourceSO resourceSO in resourceRegistrySO.resourceSOList)
        {
            if (resourceSO.resourceType != ResourceType.None)
            {
                BuildResourceField(resourceSO);
            }
        }
    }

    /// <summary>
    /// Instantiates and initializes a single resource field UI element.
    /// </summary>
    /// <param name="resourceSO">Resource definition to display.</param>
    private void BuildResourceField(ResourceSO resourceSO)
    {
        RectTransform resourceTransform = Instantiate(resourceFieldTemplate, resourceFieldContainer);
        resourceTransform.gameObject.SetActive(true);

        ResourceFieldUI resourceField = resourceTransform.GetComponent<ResourceFieldUI>();
        resourceField.Initialize(resourceSO);
        resourceFieldDictionary[resourceSO] = resourceField;
    }

    /// <summary>
    /// Updates all resource field values to reflect current resource amounts.
    /// </summary>
    private void UpdateResourceValues()
    {
        foreach (ResourceSO resourceSO in resourceRegistrySO.resourceSOList)
        {
            if (resourceSO.resourceType != ResourceType.None)
            {
                resourceFieldDictionary[resourceSO].UpdateAmount(ResourceManager.Instance.GetResourceAmount(resourceSO.resourceKey));
            }
        }
    }
}
