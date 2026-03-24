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
    [SerializeField] private RectTransform productionQueueContainer;
    [SerializeField] private Button productionQueueButtonTemplate;
    [SerializeField] private Image progressBarImage;
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
        productionQueueButtonTemplate.gameObject.SetActive(false);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        UnitSelectionManager.Instance.OnSelectionChange += UnitSelectionManager_OnSelectionChange;
        DOTSEventManager.Instance.OnTrainerUnitQueueChange += DOTSEventManager_OnUnitQueueChange;

        SetVisible(false);
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
            SetVisible(true);
            UpdateBuildingUI();
        }
        else
        {
            trainerEntity = Entity.Null;
            SetVisible(false);
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
        UpdateProgressBarVisual();
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
                child.gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
                Destroy(child.gameObject);
            }
        }

        DynamicBuffer<TrainableEntry> trainableRosterBuffer =
                entityManager.GetBuffer<TrainableEntry>(trainerEntity, isReadOnly: true);

        foreach (TrainableEntry queuedUnit in trainableRosterBuffer)
        {
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
        Debug.Log($"Running foreach on UpdateUnitQueueVisual()");
        foreach (Transform child in productionQueueContainer)
        {
            if (child.gameObject == productionQueueButtonTemplate.gameObject)
            {
                continue;
            }
            else
            {
                child.gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
                Destroy(child.gameObject);
            }
        }
        Debug.Log($"Finished foreach on UpdateUnitQueueVisual()");

        DynamicBuffer<QueuedUnitBuffer> trainerQueueBuffer =
                entityManager.GetBuffer<QueuedUnitBuffer>(trainerEntity, isReadOnly: true);

        for (int queueIndex = 0; queueIndex < trainerQueueBuffer.Length; queueIndex++)
        {
            QueuedUnitBuffer queuedUnit = trainerQueueBuffer[queueIndex];
            Button unitQueueButton = Instantiate(productionQueueButtonTemplate, parent: productionQueueContainer);
            UnitDataSO unitDataSO = GameAssets.Instance.unitRegistrySO.GetUnitSO(queuedUnit.unitKey);

            SetUnitCard(unitDataSO, unitQueueButton.gameObject);

            AddQueueButtonListener(queueIndex, unitQueueButton);

            unitQueueButton.gameObject.SetActive(true);
        }
    }

    private void AddQueueButtonListener(
        int queueIndex,
        Button unitQueueButton)
    {
        unitQueueButton.onClick.RemoveAllListeners();
        unitQueueButton.onClick.AddListener(() =>
        {
            if (trainerEntity == Entity.Null || !entityManager.Exists(trainerEntity))
            {
                return;
            }

            DynamicBuffer<QueuedUnitBuffer> trainerQueueBuffer =
                entityManager.GetBuffer<QueuedUnitBuffer>(trainerEntity, isReadOnly: false);

            //Removes unit from buffer
            if (queueIndex >= 0 && queueIndex < trainerQueueBuffer.Length)
            {
                trainerQueueBuffer.RemoveAt(queueIndex);
            }

            //Reset progress if the unit currently training is cancelled
            if (queueIndex == 0)
            {
                SetProgressToZero();
            }

            UpdateProgressBarVisual();
            UpdateUnitQueueVisual();
        });
    }

    private void SetProgressToZero()
    {
        Trainer trainer = entityManager.GetComponentData<Trainer>(trainerEntity);

        trainer.currentProgress = 0;
        entityManager.SetComponentData<Trainer>(trainerEntity, trainer);
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

    private void SetVisible(bool value)
    {
        gameObject.SetActive(value);
    }
}
