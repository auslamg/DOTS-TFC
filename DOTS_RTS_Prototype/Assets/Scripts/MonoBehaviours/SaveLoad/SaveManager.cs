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
using static SaveGameSerializer;

/// <summary>
/// Responsible for serializing current game state (ECS world + managed systems) into persistent storage.
/// </summary>
/// <remarks>
/// Supports both JSON and binary formats. Extracts data from ECS entities and writes DTO representations for units, buildings, resources, and managed camera state.
/// </remarks>
public class SaveManager : MonoBehaviour
{
    [Header("Save path settings")]

    /// <summary>
    /// Base file name used to construct save file paths.
    /// </summary>
    [SerializeField]
    [Tooltip("File name for the save file.")]
    private string fileName;

    /// <summary>
    /// Full path for JSON save file derived from <see cref="fileName"/>.
    /// </summary>
    private string jsonSavePath =>
        Path.Combine(
            Application.persistentDataPath,
            Path.GetFileNameWithoutExtension(fileName) + ".json");

    /// <summary>
    /// Full path for binary save file derived from <see cref="fileName"/>.
    /// </summary>
    private string binarySavePath =>
        Path.Combine(
            Application.persistentDataPath,
            Path.GetFileNameWithoutExtension(fileName) + ".dat");

    [Header("Save file type: true for JSON, false for .dat.")]
    /// <summary>
    /// Determines whether JSON format is used instead of binary format.
    /// </summary>
    [SerializeField]
    private bool isJson;

    [Header("References")]
    /// <summary>
    /// Camera transform used to persist camera position and rotation.
    /// </summary>
    [SerializeField]
    [Tooltip("Camera controller gizmo for camera position storage.")]
    private Transform cameraControllerGizmo;

    /// <summary>
    /// Cached ECS entity manager used to query and extract game state.
    /// </summary>
    private EntityManager entityManager;

    /// <summary>
    /// Global singleton instance of <see cref="SaveManager"/>.
    /// </summary>
    public static SaveManager Instance { get; private set; }

    /// <summary>
    /// Ensures singleton instance validity.
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

    /// <summary>
    /// Unity Awake callback. Initializes singleton instance.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// Saves the current game state to disk using the configured format (JSON or binary).
    /// </summary>
    /// <returns>True if save succeeded; otherwise false.</returns>
    public bool SaveGame()
    {
        Debug.Log("[SaveManager] SAVING...");
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        return isJson ? WriteJsonSaveFile() : WriteBinarySaveFile();
    }

    /// <summary>
    /// Writes game state to a binary file.
    /// </summary>
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
            SerializeToBinary(saveGame, stream);

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

    /// <summary>
    /// Writes game state to a JSON file.
    /// </summary>
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

            string json = SaveGameSerializer.SerializeToJson(saveGame);

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

    /// <summary>
    /// Extracts all unit entities from ECS and converts them into DTO format.
    /// </summary>
    private List<DtoUnitData> GetAllUnitData()
    {
        Debug.Log("[SaveManager] Reading UNITS...");

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Unit>()
            .Build(entityManager);

        List<DtoUnitData> savedUnitsSet = new List<DtoUnitData>();

        using var entityArray = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entityArray)
        {
            UnitDataSOHolder unitDataSOHolder = entityManager.GetComponentData<UnitDataSOHolder>(entity);

            LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entity);
            Unit unit = entityManager.GetComponentData<Unit>(entity);
            Faction faction = entityManager.GetComponentData<Faction>(entity);
            bool selected = entityManager.HasComponent<Selected>(entity) &&
                entityManager.IsComponentEnabled<Selected>(entity);

            bool requirePathing =
                entityManager.IsComponentEnabled<ManualMove>(entity) ||
                entityManager.IsComponentEnabled<FlowFieldFollower>(entity);

            UnitMover unitMover = entityManager.GetComponentData<UnitMover>(entity);
            ManualMove manualMove = entityManager.GetComponentData<ManualMove>(entity);
            FlowFieldFollower flowFieldFollower = entityManager.GetComponentData<FlowFieldFollower>(entity);
            ManualTarget manualTarget = entityManager.GetComponentData<ManualTarget>(entity);
            Health health = entityManager.GetComponentData<Health>(entity);

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

            savedUnitsSet.Add(unitData);
            Debug.Log($"[SaveManager] Saving unit: {unitData}");
        }

        return savedUnitsSet;
    }

    /// <summary>
    /// Extracts all building entities from ECS and converts them into DTO format.
    /// </summary>
    private List<DtoBuildingData> GetAllBuildingData()
    {
        Debug.Log("[SaveManager] Reading BUILDINGS...");

        EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Building>()
            .Build(entityManager);

        List<DtoBuildingData> savedBuildingsSet = new List<DtoBuildingData>();

        NativeArray<Entity> entityArray = query.ToEntityArray(Allocator.Temp);
        foreach (var entity in entityArray)
        {
            BuildingDataSOHolder buildingDataSOHolder = entityManager.GetComponentData<BuildingDataSOHolder>(entity);

            LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entity);
            Building building = entityManager.GetComponentData<Building>(entity);
            Faction faction = entityManager.GetComponentData<Faction>(entity);
            bool selected = entityManager.HasComponent<Selected>(entity) &&
                entityManager.IsComponentEnabled<Selected>(entity);
            Health health = entityManager.GetComponentData<Health>(entity);

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

            if (entityManager.HasComponent<Trainer>(entity) &&
                entityManager.HasBuffer<QueuedUnitBuffer>(entity))
            {
                Trainer trainer = entityManager.GetComponentData<Trainer>(entity);
                DynamicBuffer<QueuedUnitBuffer> unitQueueBuffer =
                    entityManager.GetBuffer<QueuedUnitBuffer>(entity, isReadOnly: true);

                buildingData.trainerData = DtoTrainerData.FromTrainer(trainer, unitQueueBuffer);
            }

            savedBuildingsSet.Add(buildingData);
            Debug.Log($"[SaveManager] Saving building: {buildingData}");
        }

        return savedBuildingsSet;
    }

    /// <summary>
    /// Extracts all resource values from the ResourceManager.
    /// </summary>
    private DtoResourceData GetAllResourceData()
    {
        var resourceAmountDictionary = ResourceManager.Instance.resourceAmountDictionary;
        return DtoResourceData.FromDictionary(resourceAmountDictionary);
    }

    /// <summary>
    /// Extracts managed (non-ECS) game state such as camera transform.
    /// </summary>
    private DtoManagedData GetManagedData()
    {
        float3 camPosition = cameraControllerGizmo.position;
        quaternion camRotation = cameraControllerGizmo.rotation;
        DtoManagedData saveManagedData = new DtoManagedData
        {
            camera = new DtoCameraData
            {
                camPosition = camPosition,
                camRotation = camRotation
            },
            horde = GetHordeData()
        };

        Debug.Log($"[SaveManager] Saving managed data: {saveManagedData}");
        return saveManagedData;
    }

    /// <summary>
    /// Builds a DtoHordeData object from the current HordeManager state.
    /// </summary>
    private DtoHordeData GetHordeData()
    {
        if (HordeManager.Instance == null)
        {
            return new DtoHordeData
            {
                currentWaveIndex = 0,
                currentState = 0,
                currentStateTimer = 0f,
                currentTimerInterval = 0f,
                isCountingDownToNextWave = false,
                remainingNextWaveTime = 0f,
                nextWaveInterval = 0f,
                currentSpawnEntryIndex = -1,
                currentEntryIndex = -1,
                currentSpawnedInEntry = 0,
                spawnEntryRemainingInterval = 0f,
                spawnEntryPostCooldownRemaining = 0f,
                finalWave = false,
                lastPoolIndex = -1
            };
        }

        var hm = HordeManager.Instance;
        return new DtoHordeData
        {
            currentWaveIndex = hm.currentWaveIndex,
            currentState = hm.CurrentState,
            currentStateTimer = hm.CurrentStateTimer,
            currentTimerInterval = hm.CurrentTimerInterval,
            isCountingDownToNextWave = hm.isCountingDownToNextWave,
            remainingNextWaveTime = hm.remainingNextWaveTime,
            nextWaveInterval = hm.nextWaveInterval,
            currentSpawnEntryIndex = hm.currentSpawnEntryIndex,
            currentEntryIndex = hm.currentEntryIndex,
            currentSpawnedInEntry = hm.currentSpawnedInEntry,
            spawnEntryRemainingInterval = hm.spawnEntryRemainingInterval,
            spawnEntryPostCooldownRemaining = hm.spawnEntryPostCooldownRemaining,
            finalWave = hm.finalWave,
            lastPoolIndex = hm.LastPoolIndex
        };
    }
}