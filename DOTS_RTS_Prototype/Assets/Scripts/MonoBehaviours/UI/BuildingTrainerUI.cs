using System;
using Unity.Collections;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BuildingTrainerUI : MonoBehaviour
{
    [Header("DOTS access")]
    [SerializeField] private Entity trainerEntity;
    /* [SerializeField] private Button soldierButton; */
    [Header("Training roster")]
    [SerializeField] private RectTransform trainingButtonContainer;
    [SerializeField] private Button trainingButtonTemplate;
    [SerializeField] private Sprite placeholderTrainButtonImage;
    [Header("Production queue")]
    [SerializeField] private Image progressBarImage;
    [SerializeField] private RectTransform productionQueueContainer;
    [SerializeField] private RectTransform productionQueueTemplate;
    //REVIEW: May use two different images. Implement if so
    /* [SerializeField] private Sprite placeholderProductionQueueImage; */

    /* [SerializeField] private string spawnedEntityKey; */
    EntityManager entityManager;

    void Awake()
    {
        //TODO: Move this to every refresh? //Optimize
        /* soldierButton.onClick.AddListener(() =>
        {
            //Enables Unit queue
            entityManager.SetComponentData(trainerEntity, new TrainUnitRequest
            {
                unitKey = new UnitKey { name = spawnedEntityKey }
            });
            entityManager.SetComponentEnabled<TrainUnitRequest>(trainerEntity, true);
        }); */
        trainingButtonTemplate.gameObject.SetActive(false);
        productionQueueTemplate.gameObject.SetActive(false);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        UnitSelectionManager.Instance.OnSelectionChange += UnitSelectionManager_OnSelectionChange;
        DOTSEventManager.Instance.OnTrainerUnitQueueChange += DOTSEventManager_OnUnitQueueChange;

        Hide();
    }

    private void DOTSEventManager_OnUnitQueueChange(object sender, EventArgs e)
    {
        Entity entity = (Entity)sender;
        if (entity == trainerEntity)
        {
            UpdateBuildingUI();
        }
    }

    private void Update()
    {
        UpdateProgressBarVisual();
    }

    private void UnitSelectionManager_OnSelectionChange(object sender, EventArgs e)
    {
        EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Selected, Trainer>()
            .Build(entityManager);

        NativeArray<Entity> entityArray = entityQuery.ToEntityArray(Allocator.Temp);
        if (entityArray.Length > 0)
        {
            trainerEntity = entityArray[0];
            Show();
            UpdateProgressBarVisual();
            UpdateBuildingUI();
        }
        else
        {
            trainerEntity = Entity.Null;
            Hide();
        }
    }

    void UpdateProgressBarVisual()
    {
        if (trainerEntity == Entity.Null)
        {
            progressBarImage.fillAmount = 0f;
            return;
        }

        Trainer trainer = entityManager.GetComponentData<Trainer>(trainerEntity);
        if (trainer.activeUnitKey.name == "" || trainer.activeUnitKey.name == "None")
        {
            progressBarImage.fillAmount = 0f;
        }
        else
        {
            progressBarImage.fillAmount = trainer.currentProgress / trainer.maxProgress;
        }
    }

    void UpdateBuildingUI()
    {
        UpdateUnitRosterButtons();
        UpdateUnitQueueVisual();
    }

    private void UpdateUnitRosterButtons()
    {
        foreach (Transform child in trainingButtonContainer)
        {
            if (child.gameObject == trainingButtonTemplate.gameObject)
            {
                continue;
            }
            else
            {
                Debug.Log("REMOVED UNIT BUTTON");
                child.gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
                Destroy(child.gameObject);
            }
        }

        DynamicBuffer<TrainableEntry> trainableRosterBuffer =
                entityManager.GetBuffer<TrainableEntry>(trainerEntity, isReadOnly: true);

        foreach (TrainableEntry queuedUnit in trainableRosterBuffer)
        {
            Debug.Log("FOUND UNIT BUTTON");
            Button unitTrainButton = Instantiate(trainingButtonTemplate, parent: trainingButtonContainer);
            UnitDataSO unitDataSO = GameAssets.Instance.unitRegistrySO.GetUnitSO(queuedUnit.unitKey);

            SetUnitCard(unitDataSO, unitTrainButton.gameObject);

            AddTrainingButtonListener(queuedUnit, unitTrainButton);

            unitTrainButton.gameObject.SetActive(true);
        }

    }

    private void AddTrainingButtonListener(TrainableEntry queuedUnit, Button unitTrainButton)
    {
        unitTrainButton.onClick.RemoveAllListeners();
        unitTrainButton.onClick.AddListener(() =>
        {
            //Enables Unit queue
            entityManager.SetComponentData(trainerEntity, new TrainUnitRequest
            {
                unitKey = queuedUnit.unitKey
            });
            entityManager.SetComponentEnabled<TrainUnitRequest>(trainerEntity, true);
        });
    }

    private void UpdateUnitQueueVisual()
    {
        foreach (Transform child in productionQueueContainer)
        {
            if (child == productionQueueTemplate)
            {
                continue;
            }
            else
            {
                Destroy(child.gameObject);
            }
        }

        DynamicBuffer<QueuedUnitBuffer> trainerQueueBuffer =
                entityManager.GetBuffer<QueuedUnitBuffer>(trainerEntity, isReadOnly: true);

        foreach (QueuedUnitBuffer queuedUnit in trainerQueueBuffer)
        {
            RectTransform unitQueueRectTransform = Instantiate(productionQueueTemplate, parent: productionQueueContainer);
            UnitDataSO unitDataSO = GameAssets.Instance.unitRegistrySO.GetUnitSO(queuedUnit.unitKey);

            SetUnitCard(unitDataSO, unitQueueRectTransform.gameObject);
            unitQueueRectTransform.gameObject.SetActive(true);
        }
    }

    private void SetUnitCard(UnitDataSO unitDataSO, GameObject uiElement)
    {
        Image image = uiElement.GetComponent<Image>();
        if (unitDataSO != null && unitDataSO.imageCard != null)
        {
            image.sprite = unitDataSO.imageCard;
        }
        else
        {
            Debug.LogWarning($"No icon found for UnitKey '{unitDataSO.unitKey}'");
            image.sprite = placeholderTrainButtonImage;
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }


}
