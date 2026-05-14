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
    /// Button used to cancel the current building selection and clear selection state.
    /// </summary>
    [SerializeField]
    [Tooltip("Button used to cancel the current building selection and clear selection state.")]
    private RectTransform cancelButtonTemplate;

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
    /// Maximum number of building options displayed in the UI grid.
    /// </summary>
    [SerializeField]
    [Tooltip("UI grid size defining how many building options can be displayed.")]
    private int optionsGridSize = 9;

    /// <summary>
    /// Runtime cache mapping each building definition to its instantiated UI button.
    /// </summary>
    private Dictionary<BuildingDataSO, RectTransform> buildingButtonDictionary;

    /// <summary>
    /// Unity Awake callback. Initializes the UI before runtime interaction begins.
    /// </summary>
    private void Awake()
    {
        InitializeUI();
    }

    /// <summary>
    /// Initializes template state and builds one button for each buildable building entry.
    /// </summary>
    private void InitializeUI()
    {
        buildingButtonTemplate.gameObject.SetActive(false);
        buildingButtonDictionary = new Dictionary<BuildingDataSO, RectTransform>();

        ConstructBuildingRoster();
    }

    /// <summary>
    /// Unity Start callback. Subscribes to runtime events and finalizes UI setup.
    /// </summary>
    private void Start()
    {
        InitializeUI_PostBake();
    }

    /// <summary>
    /// Subscribes to active-building changes and syncs selection visuals.
    /// </summary>
    private void InitializeUI_PostBake()
    {
        BuildingPlacementManager.Instance.OnActiveBuildingDataChange += BuildingPlacementManager_OnActiveBuildingDataChange;
        ConstructCanvelButton();
        UpdateSelectedVisual();
    }

    /// <summary>
    /// Configures the cancel button to clear selection and reset active building state.
    /// </summary>
    private void ConstructCanvelButton()
    {
        Button button = cancelButtonTemplate.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            SelectionManager.Instance.DeselectAll();
            BuildingPlacementManager.Instance.activeBuildingDataSO = buildingDataRegistrySO.none;
        });
    }

    /// <summary>
    /// Builds the roster of available building buttons from the registry.
    /// </summary>
    private void ConstructBuildingRoster()
    {
        int i = 0;
        foreach (BuildingDataSO buildingDataSO in buildingDataRegistrySO.buildingDataSOList)
        {
            Debug.Log($"Constructing building roster {i}");

            if (i < optionsGridSize)
            {
                i++;
                if (buildingDataSO.isBuildable && buildingDataSO.buildingType != BuildingType.None)
                {
                    BuildButton(buildingDataSO);
                }
            }
            else
            {
                Debug.LogWarning($"Couldn't show all building options in BuidlingPlacementManagerUI");
                return;
            }
        }

        // Remaining empty buttons
        for (var j = i; j < optionsGridSize + 2; j++)
        {
            Debug.Log($"Constructing building roster {j}");
            BuildEmptyButton();
        }
    }

    /// <summary>
    /// Instantiates an empty UI slot used to fill unused grid space.
    /// </summary>
    private void BuildEmptyButton()
    {
        RectTransform buildingButton = Instantiate(buildingButtonTemplate, buildingButtonContainer);
        buildingButton.gameObject.SetActive(true);
        Image image = buildingButton.transform.GetChild(2).GetComponent<Image>();
        image.color = new Color(1, 1, 1, 0);
    }

    /// <summary>
    /// Instantiates and configures a building selection button.
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
    /// Event handler invoked when the active building selection changes.
    /// </summary>
    private void BuildingPlacementManager_OnActiveBuildingDataChange(object sender, System.EventArgs e)
    {
        UpdateSelectedVisual();
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
            GameModeManager.Instance.SetGameMode(GameMode.BuildMode);
            SelectionManager.Instance.DeselectAll();
            BuildingPlacementManager.Instance.activeBuildingDataSO = buildingData;
        });
    }

    /// <summary>
    /// Updates selection visuals for all building buttons based on the active building state.
    /// </summary>
    private void UpdateSelectedVisual()
    {
        foreach (RectTransform buildingButton in buildingButtonDictionary.Values)
        {
            SetSelected(buildingButton, false);
        }

        if (BuildingPlacementManager.Instance.activeBuildingDataSO.buildingType != BuildingType.None)
        {
            RectTransform selectedBuildingButton =
                buildingButtonDictionary[BuildingPlacementManager.Instance.activeBuildingDataSO];

            if (selectedBuildingButton != null)
            {
                SetSelected(selectedBuildingButton, true);
            }
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