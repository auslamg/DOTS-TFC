using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingPlacementManagerUI : MonoBehaviour
{

    [SerializeField] private RectTransform buildingButtonContainer;
    [SerializeField] private RectTransform buildingButtonTemplate;
    [SerializeField] private BuildingDataRegistrySO buildingDataRegistrySO;
    [SerializeField] private Sprite placeholderBuildingButtonImage;
    [SerializeField] private Dictionary<BuildingDataSO, RectTransform> buildingButtonDictionary;


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

    private void Start()
    {
        BuildingPlacementManager.Instance.OnActiveBuildingDataChange += BuildingPlacementManager_OnActiveBuildingDataChange;
        UpdateSelectedVisual();
    }

    private void BuildingPlacementManager_OnActiveBuildingDataChange(object sender, System.EventArgs e)
    {
        UpdateSelectedVisual();
    }

    private void BuildButton(BuildingDataSO buildingDataSo)
    {
        RectTransform buildingButton = Instantiate(buildingButtonTemplate, buildingButtonContainer);
        buildingButton.gameObject.SetActive(true);

        SetBuildingCard(buildingDataSo, buildingButton);
        AddBuildingButtonListener(buildingDataSo, buildingButton);
        buildingButtonDictionary[buildingDataSo] = buildingButton;
    }

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

    private void AddBuildingButtonListener(BuildingDataSO buildingData, RectTransform buildingButton)
    {
        Button button = buildingButton.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            BuildingPlacementManager.Instance.ActiveBuildingDataSO = buildingData;
        });
    }

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

    private void SetSelected(RectTransform buildingButton, bool value)
    {
        Transform outlineTransform = buildingButton.transform.GetChild(1);
        outlineTransform.gameObject.SetActive(value);
    }
}
