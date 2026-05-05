using System;
using System.Collections.Generic;
using System.IO;
using Dto;
using Dto.Buildings;
using Dto.Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    [Header("Save path settings")]

    /// <summary>
    /// File name for the save file.
    /// </summary>
    [SerializeField]
    [Tooltip("File name for the save file.")]
    private string fileName;

    private string savePath => Path.Combine(Application.persistentDataPath, fileName);

    [Header("References")]
    /// <summary>
    /// Camera controller gizmo for camera position storage.
    /// </summary>
    [SerializeField]
    [Tooltip("Camera controller gizmo for camera position storage.")]
    private Transform cameraControllerGizmo;

    EntityManager entityManager;

    /// <summary>
    /// Global singleton access to the DOTS event bridge.
    /// </summary>
    public static SaveManager Instance { get; private set; }

    /// <summary>
    /// Initializes singleton instance state.
    /// </summary>
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple instances of singleton found on " + this.gameObject.name);
            Destroy(this);
        }
    }

    private void Awake()
    {
        InitializeSingleton();
    }

    public bool SaveGame()
    {
        Debug.Log("[SaveManager] SAVING...");
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        return WriteSaveFile();
    }

    private bool WriteSaveFile()
    {
        Debug.Log("[SaveManager] Writing save file...");

        try
        {
            DtoGameData saveGame = new DtoGameData
            {
                units = new List<DtoUnitData>(GetAllUnitData()),
                buildings = new List<DtoBuildingData>(GetAllBuildingData()),
                resources = GetAllResourceData(),
                managed = GetManagedData()
            };

            string json = JsonUtility.ToJson(saveGame, true);

            // Ensure directory exists (important on first run / mobile)
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));

            File.WriteAllText(savePath, json);

            Debug.Log($"[SaveManager] Save written successfully to: {savePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to write save file: {e}");
            return false;
        }
    }

    private List<DtoUnitData> GetAllUnitData()
    {
        Debug.Log("[SaveManager] Reading UNITS...");

        // Query all entities with the Unit component.
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).
            WithAll<Unit>().
            Build(entityManager);

        List<DtoUnitData> savedUnitsSet = new List<DtoUnitData>();

        // Read all entities.
        using var entityArray = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entityArray)
        {
            // Get prefab.
            UnitDataSOHolder unitDataSOHolder = entityManager.GetComponentData<UnitDataSOHolder>(entity);

            // Save dynamic data.
            LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entity);

            Unit unit = entityManager.GetComponentData<Unit>(entity);
            Faction faction = entityManager.GetComponentData<Faction>(entity);
            bool selected = entityManager.IsComponentEnabled<Selected>(entity);

            bool requirePathing =
                            entityManager.IsComponentEnabled<ManualMove>(entity) ||
                            entityManager.IsComponentEnabled<FlowFieldFollower>(entity);
            UnitMover unitMover = entityManager.GetComponentData<UnitMover>(entity);
            ManualMove manualMove = entityManager.GetComponentData<ManualMove>(entity);
            FlowFieldFollower flowFieldFollower = entityManager.GetComponentData<FlowFieldFollower>(entity);
            
            ManualTarget manualTarget = entityManager.GetComponentData<ManualTarget>(entity);
            Health health = entityManager.GetComponentData<Health>(entity);
            
            // Construct unit data structure.
            DtoUnitData unitData = new DtoUnitData
            {
                position = localTransform.Position,
                rotation = localTransform.Rotation,

                prefabKey = unitDataSOHolder.unitKey.name.ToString(),
                ownerID = unit.ownerID,
                factionID = faction.factionID,
                selected = selected,

                unitMoverPosition = unitMover.targetPosition,
                requirePathing = requirePathing,
                targetPosition = manualMove.targetPosition,
                postFormationPosition = manualMove.postFormationPosition,
                lastMoveVector = flowFieldFollower.lastMoveVector,

                targetEntity = manualTarget.targetEntity,
                currentHealth = health.currentHealth,
            };

            // Add to save set.
            savedUnitsSet.Add(unitData);
            Debug.Log($"[SaveManager] Saving unit: {unitData}");
        }

        return savedUnitsSet;
    }

    private List<DtoBuildingData> GetAllBuildingData()
    {
        Debug.Log("[SaveManager] Reading BUILDINGS...");

        // Query all entities with the Building component.
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).
            WithAll<Building>().
            Build(entityManager);

        List<DtoBuildingData> savedBuildingsSet = new List<DtoBuildingData>();

        // Read all entities.
        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entityArray)
        {
            // Get prefab.
            BuildingDataSOHolder buildingDataSOHolder = entityManager.GetComponentData<BuildingDataSOHolder>(entity);

            // Save dynamic data.
            LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entity);

            Building building = entityManager.GetComponentData<Building>(entity);
            Faction faction = entityManager.GetComponentData<Faction>(entity);
            bool selected = entityManager.IsComponentEnabled<Selected>(entity);

            Health health = entityManager.GetComponentData<Health>(entity);

            // Construct unit data structure.
            DtoBuildingData buildingData = new DtoBuildingData
            {
                position = localTransform.Position,
                rotation = localTransform.Rotation,

                prefabKey = buildingDataSOHolder.buildingKey.name.ToString(),
                ownerID = building.ownerID,
                factionID = faction.factionID,
                selected = selected,

                currentHealth = health.currentHealth,
            };

            // Read trainer data if necessary
            if (entityManager.HasComponent<Trainer>(entity) && 
                entityManager.HasBuffer<QueuedUnitBuffer>(entity))
            {
                Trainer trainer = entityManager.GetComponentData<Trainer>(entity);
                DynamicBuffer<QueuedUnitBuffer> unitQueueBuffer =
                        entityManager.GetBuffer<QueuedUnitBuffer>(entity, isReadOnly: true);
                buildingData.trainerData = DtoTrainerData.FromTrainer(trainer, unitQueueBuffer);
            }           

            // Add to save set.
            savedBuildingsSet.Add(buildingData);
            Debug.Log($"[SaveManager] Saving building: {buildingData}");
        }

        return savedBuildingsSet;
    }

    private DtoResourceData GetAllResourceData()
    {
        var resourceAmountDictionary = ResourceManager.Instance.resourceAmountDictionary;

        return DtoResourceData.FromDictionary(resourceAmountDictionary);
    }

    private DtoManagedData GetManagedData()
    {
        float3 camPosition = cameraControllerGizmo.position;
        quaternion camRotation = cameraControllerGizmo.rotation;

        DtoManagedData saveManagedData = new DtoManagedData
        {
            camPosition = camPosition,
            camRotation = camRotation
        };


        Debug.Log($"[SaveManager] Saving managed data: {saveManagedData}");
        return saveManagedData;
    }
}
