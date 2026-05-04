using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    EntityManager entityManager;

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

    public void SaveGame()
    {
        Debug.Log("[SaveManager] SAVING...");
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        List<SaveUnitData> savedUnitsSet = GetAllUnitData();
        List<SaveBuildingData> savedBuildingsSet = GetAllBuildingData();
        var saveResourceData = GetAllResourceData();
        var managedData = GetManagedData();

        WriteSaveFile();
    }

    private List<SaveUnitData> GetAllUnitData()
    {
        Debug.Log("[SaveManager] Reading UNITS...");

        //Query all entities with the Selected component to disable it
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).
            WithAll<Unit>().
            Build(entityManager);

        List<SaveUnitData> savedUnitsSet = new List<SaveUnitData>();

        // Read all entities.
        using var entityArray = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entityArray)
        {
            // Get prefab.
            UnitDataSOHolder unitDataSOHolder = entityManager.GetComponentData<UnitDataSOHolder>(entity);

            // Save post-write data.
            LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entity);
            Unit unit = entityManager.GetComponentData<Unit>(entity);
            Faction faction = entityManager.GetComponentData<Faction>(entity);

            UnitMover unitMover = entityManager.GetComponentData<UnitMover>(entity);
            ManualMove manualMove = entityManager.GetComponentData<ManualMove>(entity);
            FlowFieldFollower flowFieldFollower = entityManager.GetComponentData<FlowFieldFollower>(entity);
            
            ManualTarget manualTarget = entityManager.GetComponentData<ManualTarget>(entity);
            Health health = entityManager.GetComponentData<Health>(entity);

            bool requirePathing =
                entityManager.IsComponentEnabled<ManualMove>(entity) ||
                entityManager.IsComponentEnabled<FlowFieldFollower>(entity);

            // Construct unit data structure.
            SaveUnitData unitData = new SaveUnitData
            {
                position = localTransform.Position,
                rotation = localTransform.Rotation,

                prefabKey = unitDataSOHolder.unitKey.name.ToString(),
                ownerID = unit.ownerID,
                factionID = faction.factionID,

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

    private List<SaveBuildingData> GetAllBuildingData()
    {
        Debug.Log("[SaveManager] Reading BUILDINGS...");

        //Query all entities with the Selected component to disable it
        EntityQuery query = new EntityQueryBuilder(Allocator.Temp).
            WithAll<Building>().
            Build(entityManager);

        List<SaveBuildingData> savedBuildingsSet = new List<SaveBuildingData>();

        // Read all entities.
        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entityArray)
        {
            // Get prefab.
            BuildingDataSOHolder buildingDataSOHolder = entityManager.GetComponentData<BuildingDataSOHolder>(entity);

            // Save post-write data.
            LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entity);
            Building building = entityManager.GetComponentData<Building>(entity);
            Faction faction = entityManager.GetComponentData<Faction>(entity);
            Health health = entityManager.GetComponentData<Health>(entity);

            // Construct unit data structure.
            SaveBuildingData buildingData = new SaveBuildingData
            {
                position = localTransform.Position,
                rotation = localTransform.Rotation,

                prefabKey = buildingDataSOHolder.buildingKey.name.ToString(),
                ownerID = building.ownerID,
                factionID = faction.factionID,
                
                currentHealth = health.currentHealth,
            };

            // Add to save set.
            savedBuildingsSet.Add(buildingData);
            Debug.Log($"[SaveManager] Saving building: {buildingData}");
        }

        return savedBuildingsSet;
    }

    private SaveResourceData GetAllResourceData()
    {
        var resourceAmountDictionary = ResourceManager.Instance.resourceAmountDictionary;

        return SaveResourceData.FromDictionary(resourceAmountDictionary);
    }

    private SaveManagedData GetManagedData()
    {
        float3 camPosition = cameraControllerGizmo.position;
        quaternion camRotation = cameraControllerGizmo.rotation;

        SaveManagedData saveManagedData = new SaveManagedData
        {
            camPosition = camPosition,
            camRotation = camRotation
        };


        Debug.Log($"[SaveManager] Saving managed data: {saveManagedData}");
        return saveManagedData;
    }

    private void WriteSaveFile()
    {
        Debug.Log("[SaveManager] Writing save file...");

        try
        {
            SaveGameData saveGame = new SaveGameData
            {
                units = new List<SaveUnitData>(GetAllUnitData()),
                buildings = new List<SaveBuildingData>(GetAllBuildingData()),
                resources = GetAllResourceData(),
                managed = GetManagedData()
            };

            string json = JsonUtility.ToJson(saveGame, true);

            // Ensure directory exists (important on first run / mobile)
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));

            File.WriteAllText(savePath, json);

            Debug.Log($"[SaveManager] Save written successfully to: {savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to write save file: {e}");
        }
    }
}
