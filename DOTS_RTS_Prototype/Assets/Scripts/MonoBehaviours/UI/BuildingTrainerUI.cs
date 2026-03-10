using System;
using Unity.Collections;
using Unity.Entities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BuildingTrainerUI : MonoBehaviour
{
    [SerializeField] private Button soldierButton;
    [SerializeField] private Image progressBarImage;
    [SerializeField] private RectTransform productionQueueContainer;
    [SerializeField] private RectTransform productionQueueTemplate;

    [SerializeField] private Entity trainerEntity;
    [SerializeField] private string spawnedEntityKey;
    EntityManager entityManager;

    void Awake()
    {
        soldierButton.onClick.AddListener(() =>
        {
            //Enables Unit queue
            entityManager.SetComponentData(trainerEntity, new QueuedUnit
            {
                unitKey = new UnitKey { name = spawnedEntityKey }
            });
            entityManager.SetComponentEnabled<QueuedUnit>(trainerEntity, true);
        });

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
            UpdateUnitQueueVisual();
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
            UpdateUnitQueueVisual();
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

    void UpdateUnitQueueVisual()
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

        DynamicBuffer<SpawnUnitBuffer> trainerQueueBuffer =
                entityManager.GetBuffer<SpawnUnitBuffer>(trainerEntity, isReadOnly: true);

        foreach (SpawnUnitBuffer spawnUnit in trainerQueueBuffer)
        {
            RectTransform unitQueueRectTransform = Instantiate(productionQueueTemplate, parent: productionQueueContainer);
            unitQueueRectTransform.gameObject.SetActive(true);
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
