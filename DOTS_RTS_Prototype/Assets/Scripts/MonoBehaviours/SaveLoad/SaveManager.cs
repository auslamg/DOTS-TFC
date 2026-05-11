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
using static SaveLoadUtil;

public class SaveManager : MonoBehaviour
{
    [Header("Save path settings")]

    /// <summary>
    /// File name for the save file.
    /// </summary>
    [SerializeField]
    [Tooltip("File name for the save file.")]
    private string fileName;

    private string jsonSavePath =>
    Path.Combine(
        Application.persistentDataPath,
         Path.GetFileNameWithoutExtension(fileName) + ".json");

    private string binarySavePath =>
    Path.Combine(
        Application.persistentDataPath,
        Path.GetFileNameWithoutExtension(fileName) + ".dat");

    [Header("Save file type: true for JSON, false for .dat.")]
    [SerializeField]
    private bool isJson;

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

        return isJson ? WriteJsonSaveFile() : WriteBinarySaveFile();
    }

    private bool WriteBinarySaveFile()
    {
        Debug.Log("[SaveManager] Writing binary save file...");

        try
        {
            DtoGameData saveGame = new DtoGameData
            {
                units = new List<DtoUnitData>(GetAllUnitData()),
                buildings = new List<DtoBuildingData>(GetAllBuildingData()),
                resources = GetAllResourceData(),
                managed = GetManagedData()
            };

            Directory.CreateDirectory(Path.GetDirectoryName(binarySavePath));

            using FileStream stream = new FileStream(binarySavePath, FileMode.Create);
            using BinaryWriter writer = new BinaryWriter(stream);

            // MANAGED
            WriteFloat3(writer, saveGame.managed.camPosition);
            WriteQuaternion(writer, saveGame.managed.camRotation);

            // RESOURCES
            writer.Write(saveGame.resources.resources.Count);

            foreach (var r in saveGame.resources.resources)
            {
                writer.Write(r.resourceKey.name.ToString());
                writer.Write(r.amount);
            }

            // BUILDINGS
            writer.Write(saveGame.buildings.Count);

            foreach (var b in saveGame.buildings)
            {
                writer.Write(b.prefabKey);

                WriteFloat3(writer, b.position);
                WriteQuaternion(writer, b.rotation);

                writer.Write(b.ownerID);
                writer.Write(b.factionID);
                writer.Write(b.selected);
                writer.Write(b.currentHealth);

                bool hasTrainer = b.trainerData.trainingQueue != null;
                writer.Write(hasTrainer);

                if (hasTrainer)
                {
                    writer.Write(b.trainerData.currentProgress);
                    writer.Write(b.trainerData.maxProgress);
                    writer.Write(b.trainerData.activeUnitKey ?? "");

                    WriteFloat3(writer, b.trainerData.spawnPointOffset.ToFloat3());
                    WriteFloat3(writer, b.trainerData.rallyPositionOffset.ToFloat3());

                    writer.Write(b.trainerData.onUnitQueueChange);

                    writer.Write(b.trainerData.trainingQueue.Count);

                    foreach (var q in b.trainerData.trainingQueue)
                        writer.Write(q);
                }
            }

            // UNITS
            writer.Write(saveGame.units.Count);

            foreach (var u in saveGame.units)
            {
                writer.Write(u.prefabKey);

                WriteFloat3(writer, u.position);
                WriteQuaternion(writer, u.rotation);

                writer.Write(u.ownerID);
                writer.Write(u.factionID);
                writer.Write(u.selected);
                writer.Write(u.requirePathing);

                WriteFloat3(writer, u.unitMoverPosition);
                WriteFloat3(writer, u.targetPosition);
                WriteFloat3(writer, u.postFormationPosition);
                WriteFloat3(writer, u.lastMoveVector);

                writer.Write(u.targetEntity.Index);
                writer.Write(u.targetEntity.Version);

                writer.Write(u.currentHealth);
            }

            Debug.Log($"[SaveManager] Binary save written: {binarySavePath}");
            return true;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.LogError($"[SaveManager] Binary save failed - no write permission: {e.Message}");
            return false;
        }
        catch (DirectoryNotFoundException e)
        {
            Debug.LogError($"[SaveManager] Binary save failed - invalid path: {e.Message}");
            return false;
        }
        catch (PathTooLongException e)
        {
            Debug.LogError($"[SaveManager] Binary save failed - path too long: {e.Message}");
            return false;
        }
        catch (IOException e)
        {
            Debug.LogError($"[SaveManager] Binary save failed - IO error (disk full / locked file): {e.Message}");
            return false;
        }
        catch (NullReferenceException e)
        {
            Debug.LogError($"[SaveManager] Binary save failed - null data in DTO or ECS read: {e.Message}");
            return false;
        }
        catch (ArgumentException e)
        {
            Debug.LogError($"[SaveManager] Binary save failed - invalid argument or corrupted data: {e.Message}");
            return false;
        }
        catch (ObjectDisposedException e)
        {
            Debug.LogError($"[SaveManager] Binary save failed - stream disposed early: {e.Message}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Binary save failed - unexpected error: {e}");
            return false;
        }
    }

    private bool WriteJsonSaveFile()
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
            Directory.CreateDirectory(Path.GetDirectoryName(jsonSavePath));

            File.WriteAllText(jsonSavePath, json);

            Debug.Log($"[SaveManager] Save written successfully to: {jsonSavePath}");
            return true;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.LogError($"[SaveManager] JSON save failed - no write permission: {e.Message}");
            return false;
        }
        catch (DirectoryNotFoundException e)
        {
            Debug.LogError($"[SaveManager] JSON save failed - invalid directory: {e.Message}");
            return false;
        }
        catch (IOException e)
        {
            Debug.LogError($"[SaveManager] JSON save failed - IO issue (disk full / locked file): {e.Message}");
            return false;
        }
        catch (ArgumentException e)
        {
            Debug.LogError($"[SaveManager] JSON save failed - invalid path or data: {e.Message}");
            return false;
        }
        catch (NullReferenceException e)
        {
            Debug.LogError($"[SaveManager] JSON save failed - null DTO field: {e.Message}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] JSON save failed - unexpected error: {e}");
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
