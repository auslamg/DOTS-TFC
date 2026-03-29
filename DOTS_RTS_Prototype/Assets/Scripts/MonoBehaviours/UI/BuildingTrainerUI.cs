using System;
using Unity.Collections;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls trainer-building UI: trainable roster, queued units, and training progress.
/// </summary>
/// <remarks>
/// This component bridges DOTS trainer data and the classic UI hierarchy. It listens to
/// selection and queue-change events, rebuilds button lists from ECS buffers, and sends
/// training/cancel requests back to ECS through component updates.
/// </remarks>
public class BuildingTrainerUI : MonoBehaviour
{
    [Header("DOTS access")]
    /// <summary>
    /// Currently selected trainer entity used as UI data source.
    /// </summary>
    [SerializeField]
    [Tooltip("Currently selected trainer entity used as source for roster, queue, and progress UI.")]
    private Entity trainerEntity;
    /* [SerializeField] private Button soldierButton; */
    [Header("Training roster")]
    /// <summary>
    /// Container where trainable-unit buttons are instantiated.
    /// </summary>
    [SerializeField]
    [Tooltip("Container where trainable-unit buttons are instantiated.")]
    private RectTransform trainingButtonContainer;

    /// <summary>
    /// Template used for trainable-unit buttons.
    /// </summary>
    [SerializeField]
    [Tooltip("Template button used for each trainable unit entry.")]
    private Button trainingButtonTemplate;

    /// <summary>
    /// Fallback sprite used when a unit has no configured card image.
    /// </summary>
    [SerializeField]
    [Tooltip("Fallback sprite used when a trainable unit has no card image.")]
    private Sprite placeholderTrainButtonImage;

    [Header("Production queue")]
    /// <summary>
    /// Container where queued-unit buttons are instantiated.
    /// </summary>
    [SerializeField]
    [Tooltip("Container where queued-unit buttons are instantiated.")]
    private RectTransform productionQueueContainer;

    /// <summary>
    /// Template used for queued-unit buttons.
    /// </summary>
    [SerializeField]
    [Tooltip("Template button used for each queued unit entry.")]
    private Button productionQueueButtonTemplate;

    /// <summary>
    /// Fill image that displays active training progress.
    /// </summary>
    [SerializeField]
    [Tooltip("Progress bar image whose fill amount reflects current training progress.")]
    private Image progressBarImage;
    //REVIEW: May use two different images. Implement if so
    /* [SerializeField] private Sprite placeholderProductionQueueImage; */

    /* [SerializeField] private string spawnedEntityKey; */
    /// <summary>
    /// Cached EntityManager used for reading and writing trainer ECS data.
    /// </summary>
    EntityManager entityManager;

    /// <summary>
    /// Initializes template visibility before first UI build.
    /// </summary>
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

    /// <summary>
    /// Caches ECS access, subscribes to relevant events, and hides the panel until a trainer is selected.
    /// </summary>
    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        UnitSelectionManager.Instance.OnSelectionChange += UnitSelectionManager_OnSelectionChange;
        DOTSEventManager.Instance.OnTrainerUnitQueueChange += DOTSEventManager_OnUnitQueueChange;

        SetVisible(false);
    }

    /// <summary>
    /// Handles trainer queue-change events and refreshes UI when the active trainer changed.
    /// </summary>
    /// <param name="sender">Trainer entity that triggered the event.</param>
    /// <param name="e">Unused event payload.</param>
    private void DOTSEventManager_OnUnitQueueChange(object sender, EventArgs e)
    {
        Entity entity = (Entity)sender;
        if (entity == trainerEntity)
        {
            UpdateBuildingUI();
        }
    }

    /// <summary>
    /// Updates the progress bar every frame to keep visual progress in sync.
    /// </summary>
    private void Update()
    {
        UpdateProgressBarVisual();
    }

    /// <summary>
    /// Resolves the selected trainer entity and toggles panel visibility accordingly.
    /// </summary>
    /// <param name="sender">Unused event sender.</param>
    /// <param name="e">Unused event payload.</param>
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

    /// <summary>
    /// Updates progress bar fill from current trainer state.
    /// </summary>
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

    /// <summary>
    /// Rebuilds all trainer-related UI sections from ECS data.
    /// </summary>
    void UpdateBuildingUI()
    {
        UpdateProgressBarVisual();
        UpdateUnitRosterButtons();
        UpdateUnitQueueVisual();
    }

    /// <summary>
    /// Recreates roster buttons from the trainer's <see cref="TrainableEntry"/> buffer.
    /// </summary>
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

    /// <summary>
    /// Wires a train button to enqueue a unit request in ECS.
    /// </summary>
    /// <param name="queuedUnit">Unit entry represented by the button.</param>
    /// <param name="unitTrainButton">Button instance to wire.</param>
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

    /// <summary>
    /// Recreates queue buttons from the trainer's <see cref="QueuedUnitBuffer"/>.
    /// </summary>
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

    /// <summary>
    /// Wires a queue button to remove its corresponding queued unit entry.
    /// </summary>
    /// <param name="queueIndex">Queue index represented by the button.</param>
    /// <param name="unitQueueButton">Button instance to wire.</param>
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

    /// <summary>
    /// Resets active trainer progress to zero in ECS.
    /// </summary>
    private void SetProgressToZero()
    {
        Trainer trainer = entityManager.GetComponentData<Trainer>(trainerEntity);

        trainer.currentProgress = 0;
        entityManager.SetComponentData<Trainer>(trainerEntity, trainer);
    }

    /// <summary>
    /// Applies unit card art to a UI element, using a fallback sprite when needed.
    /// </summary>
    /// <param name="unitDataSO">Unit definition used to resolve card sprite.</param>
    /// <param name="uiElement">UI object whose image component is updated.</param>
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

    /// <summary>
    /// Shows or hides the entire trainer panel.
    /// </summary>
    /// <param name="value"><see langword="true"/> to show the panel; otherwise <see langword="false"/>.</param>
    private void SetVisible(bool value)
    {
        gameObject.SetActive(value);
    }
}
