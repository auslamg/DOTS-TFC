using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    /// Runtime cache mapping each building definition to its instantiated UI button.
    /// </summary>
    private Dictionary<ResourceSO, ResourceFieldUI> resourceFieldDictionary;

    /// <summary>
    /// Initializes template state and builds one button for each buildable building entry.
    /// </summary>
    private void Awake()
    {
        resourceFieldTemplate.gameObject.SetActive(false);
        resourceFieldDictionary = new Dictionary<ResourceSO, ResourceFieldUI>();
        InitializeUI();
    }

    private void InitializeUI()
    {
        ScrapResourceFields();
        ConstructResourceFields();
    }

    void Start()
    {
        InitializeUI_PostBake();
    }

    private void InitializeUI_PostBake()
    {
        ResourceManager.Instance.OnResourceValueChange += ResourceManager_OnResourceValueChange;
    }

    private void ResourceManager_OnResourceValueChange(object sender, EventArgs e)
    {
        UpdateResourceValues();
    }

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

    private void ConstructResourceFields()
    {
        foreach (ResourceSO resourceSO in resourceRegistrySO.resourceSOList)
        {
            BuildResourceField(resourceSO);
        }
    }

    private void BuildResourceField(ResourceSO resourceSO)
    {
        RectTransform resourceTransform = Instantiate(resourceFieldTemplate, resourceFieldContainer);
        resourceTransform.gameObject.SetActive(true);

        ResourceFieldUI resourceField = resourceTransform.GetComponent<ResourceFieldUI>();
        resourceField.Initialize(resourceSO);
        resourceFieldDictionary[resourceSO] = resourceField;
    }

    private void UpdateResourceValues()
    {
        foreach (ResourceSO resourceSO in resourceRegistrySO.resourceSOList)
        {
            resourceFieldDictionary[resourceSO].UpdateAmount(ResourceManager.Instance.GetResourceValue(resourceSO.resourceKey));
        }
    }
}
