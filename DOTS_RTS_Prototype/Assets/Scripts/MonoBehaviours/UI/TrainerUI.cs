using System;
using System.Linq;
using TMPro;
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
public class TrainerUI : MonoBehaviour
{
    [Header("DOTS access")]

    /// <summary>
    /// Currently selected trainer entity used as UI data source.
    /// </summary>
    [SerializeField]
    [Tooltip("Currently selected trainer entity used as source for roster, queue, and progress UI.")]
    private Entity trainerEntity;

    [Header("Training roster")]

    /// <summary>
    /// Container where trainable-unit buttons are instantiated.
    /// </summary>
    [SerializeField]
    [Tooltip("Container where trainable-unit buttons are instantiated.")]
    private RectTransform trainingRosterContainer;

    /// <summary>
    /// Template used for trainable-unit buttons.
    /// </summary>
    [SerializeField]
    [Tooltip("Template button used for each trainable unit entry.")]
    private Button trainingButtonTemplate;

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
    /// Template used for queued-unit buttons.
    /// </summary>
    [SerializeField]
    [Tooltip("Template button used for each queued unit entry.")]
    private Button remainderQueueButtonTemplate;

    /// <summary>
    /// Fill image that displays active training progress.
    /// </summary>
    [SerializeField]
    [Tooltip("Progress bar image whose fill amount reflects current training progress.")]
    private Image progressBarImage;

    //REVIEW: May use two different images. Implement if so
    /* [SerializeField] private Sprite placeholderProductionQueueImage; */

    /* [SerializeField] private string spawnedEntityKey; */

    [Header("Registries")]

    /// <summary>
    /// Registry containing all trainable unit definitions.
    /// </summary>
    [SerializeField]
    [Tooltip("Registry containing all trainable unit definitions.")]
    UnitDataRegistrySO unitDataRegistrySO;

    [Header("Settings")]

    /// <summary>
    /// UI Grid max size.
    /// </summary>
    [SerializeField]
    [Tooltip("UI Grid max size.")]
    private int optionsGridMaxSize = 5;


    /// <summary>
    /// Cached EntityManager used for reading and writing trainer ECS data.
    /// </summary>
    EntityManager entityManager;

    /// <summary>
    /// Initializes template visibility before first UI build.
    /// </summary>
    void Awake()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        trainingButtonTemplate.gameObject.SetActive(false);
        productionQueueButtonTemplate.gameObject.SetActive(false);
        remainderQueueButtonTemplate.gameObject.SetActive(false);
    }


    void Start()
    {
        InitializeUI_PostBake();
    }

    /// <summary>
    /// Caches ECS access, subscribes to relevant events, and hides the panel until a trainer is selected.
    /// </summary>
    private void InitializeUI_PostBake()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        UnitSelectionManager.Instance.OnSelectionChange += UnitSelectionManager_OnSelectionChange;
        DOTSEventManager.Instance.OnTrainerUnitQueueChange += DOTSEventManager_OnUnitQueueChange;
        ResourceManager.Instance.OnResourceValueChange += ResourceManager_OnResourceValueChange;

        SetVisible(false);
    }

    private void ResourceManager_OnResourceValueChange(object sender, EventArgs e)
    {
        /* throw new NotImplementedException(); */
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
            UpdateUI();
        }
    }

    /// <summary>
    /// Updates the progress bar every frame to keep visual progress in sync.
    /// </summary>
    private void Update()
    {
        UpdateProgressBar();
    }

    /// <summary>
    /// Resolves the selected trainer entity and toggles panel visibility accordingly.
    /// </summary>
    /// <param name="sender">Unused event sender.</param>
    /// <param name="e">Unused event payload.</param>
    private void UnitSelectionManager_OnSelectionChange(object sender, EventArgs e)
    {
        using EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Selected, Trainer>()
            .Build(entityManager);

        using NativeArray<Entity> entityArray = entityQuery.ToEntityArray(Allocator.Temp);
        if (entityArray.Length > 0)
        {
            trainerEntity = entityArray[0];
            SetVisible(true);
            UpdateUI();
        }
        else
        {
            trainerEntity = Entity.Null;
            SetVisible(false);
        }
    }

    /// <summary>
    /// Rebuilds all trainer-related UI sections from ECS data.
    /// </summary>
    void UpdateUI()
    {
        UpdateProgressBar();
        UpdateUnitRosterButtons();
        UpdateUnitQueueButtons();
    }

    /// <summary>
    /// Updates progress bar fill from current trainer state.
    /// </summary>
    void UpdateProgressBar()
    {
        if (!EntityUtil.ExistsAndPersists(ref entityManager, ref trainerEntity))
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
    /// Recreates roster buttons from the trainer's <see cref="TrainableEntry"/> buffer.
    /// </summary>
    private void UpdateUnitRosterButtons()
    {
        ScrapUnitRoster();
        ConstructUnitRoster();
    }

    private void ScrapUnitRoster()
    {
        foreach (Transform child in trainingRosterContainer)
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
    }

    private void ConstructUnitRoster()
    {
        DynamicBuffer<TrainableEntry> trainableRosterBuffer =
                        entityManager.GetBuffer<TrainableEntry>(trainerEntity, isReadOnly: true);

        int i = 0;
        foreach (TrainableEntry queuedUnit in trainableRosterBuffer)
        {
            if (i < optionsGridMaxSize)
            {
                i++;
                BuildUnitButton(queuedUnit);
            }
            else
            {
                Debug.LogWarning($"Couldn't show all unit options in TrainerUI");
                return;
            }
        }
    }

    private void BuildUnitButton(TrainableEntry queuedUnit)
    {
        Button unitTrainButton = Instantiate(trainingButtonTemplate, parent: trainingRosterContainer);
        UnitDataSO unitDataSO = GameAssets.Instance.unitRegistrySO.GetUnitSO(queuedUnit.unitKey);

        SetUnitCard(unitDataSO, unitTrainButton.gameObject);
        AddTrainingButtonListener(queuedUnit, unitTrainButton);

        unitTrainButton.gameObject.SetActive(true);
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
            UnitDataSO unitDataSO = unitDataRegistrySO.GetUnitSO(queuedUnit.unitKey);
            if (ResourceManager.Instance.CanSpendResourceValues(unitDataSO.constructionCost))
            {
                // Enables Unit queue.
                entityManager.SetComponentData(trainerEntity, new TrainUnitRequest
                {
                    unitKey = queuedUnit.unitKey
                });
                entityManager.SetComponentEnabled<TrainUnitRequest>(trainerEntity, true);

                // Consumes construction cost.
                ResourceManager.Instance.SpendResourceValues(unitDataSO.constructionCost);
            }
        });
    }

    /// <summary>
    /// Recreates queue buttons from the trainer's <see cref="QueuedUnitBuffer"/>.
    /// </summary>
    private void UpdateUnitQueueButtons()
    {
        ScrapUnitQueue();
        ConstructUnitQueue();
    }

    private void ScrapUnitQueue()
    {
        foreach (Transform child in productionQueueContainer)
        {
            if (child.gameObject == productionQueueButtonTemplate.gameObject ||
                child.gameObject == remainderQueueButtonTemplate.gameObject)
            {
                continue;
            }
            else
            {
                child.gameObject.GetComponent<Button>().onClick.RemoveAllListeners();
                Destroy(child.gameObject);
            }
        }
    }

    private void ConstructUnitQueue()
    {
        DynamicBuffer<QueuedUnitBuffer> trainerQueueBuffer =
                        entityManager.GetBuffer<QueuedUnitBuffer>(trainerEntity, isReadOnly: true);

        for (int queueIndex = 0; queueIndex < trainerQueueBuffer.Length; queueIndex++)
        {
            // If queue exceeds max size, draw a remainder button
            if (queueIndex == optionsGridMaxSize - 1 &&
                trainerQueueBuffer.Length > optionsGridMaxSize)
            {
                BuildRemainderQueueButton(trainerQueueBuffer.Length - queueIndex);
                Debug.Log($"Couldn't show all queued units in TrainerUI");
                return;
            }
            else
                BuildQueueButton(ref trainerQueueBuffer, queueIndex);
        }

    }

    private void BuildRemainderQueueButton(int remainingElements)
    {
        Button remainderQueueButton = Instantiate(remainderQueueButtonTemplate, parent: productionQueueContainer);
        remainderQueueButton.transform.GetChild(0).GetChild(1).GetComponent<TMP_Text>().text = remainingElements.ToString() + "+";

        remainderQueueButton.gameObject.SetActive(true);
    }

    private void BuildQueueButton(ref DynamicBuffer<QueuedUnitBuffer> trainerQueueBuffer, int queueIndex)
    {
        QueuedUnitBuffer queuedUnit = trainerQueueBuffer[queueIndex];
        Button unitQueueButton = Instantiate(productionQueueButtonTemplate, parent: productionQueueContainer);
        UnitDataSO unitDataSO = GameAssets.Instance.unitRegistrySO.GetUnitSO(queuedUnit.unitKey);

        SetUnitCard(unitDataSO, unitQueueButton.gameObject);
        AddQueueButtonListener(queueIndex, unitQueueButton);

        unitQueueButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// Wires a queue button to remove its corresponding queued unit entry.
    /// </summary>
    /// <param name="queueIndex">Queue index represented by the button.</param>
    /// <param name="unitQueueButton">Button instance to wire.</param>
    private void AddQueueButtonListener(int queueIndex, Button unitQueueButton)
    {
        unitQueueButton.onClick.RemoveAllListeners();
        unitQueueButton.onClick.AddListener(() =>
        {
            if (!EntityUtil.ExistsAndPersists(ref entityManager, ref trainerEntity))
            {
                return;
            }

            DynamicBuffer<QueuedUnitBuffer> trainerQueueBuffer =
                entityManager.GetBuffer<QueuedUnitBuffer>(trainerEntity, isReadOnly: false);

            // If actually inside the buffer.
            if (queueIndex >= 0 && queueIndex < trainerQueueBuffer.Length)
            {
                // Refund construction cost.
                UnitDataSO unitDataSO = unitDataRegistrySO.GetUnitSO(trainerQueueBuffer[queueIndex].unitKey);
                ResourceManager.Instance.AddResourceValues(unitDataSO.constructionCost);

                // Remove unit from buffer.
                trainerQueueBuffer.RemoveAt(queueIndex);
            }

            // Reset progress if the unit currently training is cancelled
            if (queueIndex == 0)
            {
                SetProgressToZero();
            }

            UpdateProgressBar();
            UpdateUnitQueueButtons();
        });
    }

    /// <summary>
    /// Resets active trainer progress to zero in ECS. //FIX: UI should not include logic
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
        Image image = uiElement.transform.GetChild(0).GetChild(0).GetComponent<Image>();
        if (unitDataSO != null && unitDataSO.imageCard != null)
        {
            image.sprite = unitDataSO.imageCard;
        }
        else
        {
            Debug.LogWarning($"No icon found for UnitKey '{unitDataSO.unitKey}'");
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

    void OnDisable()
    {
        ScrapUnitRoster();
        ScrapUnitQueue();
    }

    void OnDestroy()
    {
        ScrapUnitRoster();
        ScrapUnitQueue();
    }
}
