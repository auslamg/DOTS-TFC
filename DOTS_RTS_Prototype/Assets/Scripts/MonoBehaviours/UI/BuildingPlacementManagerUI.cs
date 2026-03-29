using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds and controls the building selection UI used by <see cref="BuildingPlacementManager"/>.
/// </summary>
/// <remarks>
/// On startup, this component creates one button per buildable <see cref="BuildingDataSO"/>,
/// assigns card visuals, and wires click events to set the active building selection.
/// It also listens to active-building change events to refresh selected outlines.
/// </remarks>
public class BuildingPlacementManagerUI : MonoBehaviour
{
    /// <summary>
    /// Parent container where building buttons are instantiated.
    /// </summary>
    [SerializeField]
    [Tooltip("Parent container where building buttons are instantiated.")]
    private RectTransform buildingButtonContainer;

    /// <summary>
    /// Template button used to instantiate one card per buildable building.
    /// </summary>
    [SerializeField]
    [Tooltip("Template button used to instantiate one card per buildable building.")]
    private RectTransform buildingButtonTemplate;

    /// <summary>
    /// Registry containing all building definitions used to populate the UI.
    /// </summary>
    [SerializeField]
    [Tooltip("Building registry used to populate selectable build cards.")]
    private BuildingDataRegistrySO buildingDataRegistrySO;

    /// <summary>
    /// Fallback sprite shown when a building has no card image configured.
    /// </summary>
    [SerializeField]
    [Tooltip("Fallback sprite used when a building has no card image.")]
    private Sprite placeholderBuildingButtonImage;

    /// <summary>
    /// Runtime cache mapping each building definition to its instantiated UI button.
    /// </summary>
    private Dictionary<BuildingDataSO, RectTransform> buildingButtonDictionary;


    /// <summary>
    /// Initializes template state and builds one button for each buildable building entry.
    /// </summary>
    private void Awake()
    {
        buildingButtonTemplate.gameObject.SetActive(false);
        buildingButtonDictionary = new Dictionary<BuildingDataSO, RectTransform>();

        foreach (BuildingDataSO buildingDataSO in buildingDataRegistrySO.buildingDataSOList)
        {
            if (buildingDataSO.isBuildable)
            {
                BuildButton(buildingDataSO);
            }
        }
    }

    /// <summary>
    /// Subscribes to active-building changes and syncs selection visuals.
    /// </summary>
    private void Start()
    {
        BuildingPlacementManager.Instance.OnActiveBuildingDataChange += BuildingPlacementManager_OnActiveBuildingDataChange;
        UpdateSelectedVisual();
    }

    /// <summary>
    /// Refreshes button selection visuals when the active building changes.
    /// </summary>
    private void BuildingPlacementManager_OnActiveBuildingDataChange(object sender, System.EventArgs e)
    {
        UpdateSelectedVisual();
    }

    /// <summary>
    /// Instantiates and wires a single building card button.
    /// </summary>
    /// <param name="buildingDataSo">Building definition represented by the button.</param>
    private void BuildButton(BuildingDataSO buildingDataSo)
    {
        RectTransform buildingButton = Instantiate(buildingButtonTemplate, buildingButtonContainer);
        buildingButton.gameObject.SetActive(true);

        SetBuildingCard(buildingDataSo, buildingButton);
        AddBuildingButtonListener(buildingDataSo, buildingButton);
        buildingButtonDictionary[buildingDataSo] = buildingButton;
    }

    /// <summary>
    /// Sets card art for a building button, using a fallback image when needed.
    /// </summary>
    /// <param name="buildingDataSO">Building definition used to resolve the card sprite.</param>
    /// <param name="buildingButton">Instantiated building button transform.</param>
    private void SetBuildingCard(BuildingDataSO buildingDataSO, RectTransform buildingButton)
    {
        Image image = buildingButton.transform.GetChild(2).GetComponent<Image>();
        Debug.Log($"Image: {image.gameObject.name}");
        if (buildingDataSO != null && buildingDataSO.imageCard != null)
        {
            image.sprite = buildingDataSO.imageCard;
        }
        else
        {
            Debug.LogWarning($"No icon found for BuildingKey '{buildingDataSO.buildingKey}'");
            image.sprite = placeholderBuildingButtonImage;
        }
    }

    /// <summary>
    /// Adds click behavior to set the clicked building as the active placement target.
    /// </summary>
    /// <param name="buildingData">Building definition represented by the button.</param>
    /// <param name="buildingButton">Button that triggers the selection change.</param>
    private void AddBuildingButtonListener(BuildingDataSO buildingData, RectTransform buildingButton)
    {
        Button button = buildingButton.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            BuildingPlacementManager.Instance.ActiveBuildingDataSO = buildingData;
        });
    }

    /// <summary>
    /// Clears all selected states and highlights the currently active building button.
    /// </summary>
    private void UpdateSelectedVisual()
    {
        foreach (RectTransform buildingButton in buildingButtonDictionary.Values)
        {
            SetSelected(buildingButton, false);
        }
        RectTransform selectedBuildingButton = buildingButtonDictionary[BuildingPlacementManager.Instance.ActiveBuildingDataSO];
        if (selectedBuildingButton != null)
        {

            SetSelected(selectedBuildingButton, true);
        }
    }

    /// <summary>
    /// Toggles the selected outline visual for a building button.
    /// </summary>
    /// <param name="buildingButton">Button whose outline should be toggled.</param>
    /// <param name="value"><see langword="true"/> to show the outline; otherwise <see langword="false"/>.</param>
    private void SetSelected(RectTransform buildingButton, bool value)
    {
        Transform outlineTransform = buildingButton.transform.GetChild(1);
        outlineTransform.gameObject.SetActive(value);
    }
}
