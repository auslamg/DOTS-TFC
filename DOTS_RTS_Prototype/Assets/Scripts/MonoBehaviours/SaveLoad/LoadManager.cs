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

/// <summary>
/// Handles loading of persisted game state from JSON or binary save files and reconstructs ECS entities accordingly.
/// </summary>
/// <remarks>
/// Responsible for deserializing save data, clearing existing world state, and rebuilding units, buildings, resources, and managed camera state.
/// </remarks>
public class LoadManager : MonoBehaviour
{
    [Header("Save path settings")]
    /// <summary>
    /// Base file name used to construct JSON and binary save paths.
    /// </summary>
    [SerializeField]
    private string fileName;

    /// <summary>
    /// Full path to JSON save file derived from <see cref="fileName"/>.
    /// </summary>
    private string jsonSavePath =>
        Path.Combine(
            Application.persistentDataPath,
            Path.GetFileNameWithoutExtension(fileName) + ".json");

    /// <summary>
    /// Full path to binary save file derived from <see cref="fileName"/>.
    /// </summary>
    private string binarySavePath =>
        Path.Combine(
            Application.persistentDataPath,
            Path.GetFileNameWithoutExtension(fileName) + ".dat");

    [Header("Load file type: true for JSON, false for .dat.")]
    /// <summary>
    /// Determines whether JSON is preferred over binary when loading.
    /// </summary>
    [SerializeField]
    private bool isJson;

    [Header("References")]
    /// <summary>
    /// Camera controller transform used to restore saved camera position and rotation.
    /// </summary>
    [SerializeField]
    [Tooltip("Camera controller gizmo for camera position storage.")]
    private Transform cameraControllerGizmo;

    /// <summary>
    /// Cached ECS entity manager used for spawning and modifying entities during load.
    /// </summary>
    private EntityManager entityManager;

    [Header("References")]
    /// <summary>
    /// Global singleton instance of <see cref="LoadManager"/>.
    /// </summary>
    public static LoadManager Instance { get; private set; }

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
    /// Checks whether a save file exists for the currently configured save type.
    /// </summary>
    /// <param name="path">Unused parameter (legacy; existence check uses internal path selection).</param>
    /// <returns>True if save file exists; otherwise false.</returns>
    public bool SaveFileExists(string path)
    {
        return isJson ? File.Exists(jsonSavePath) : File.Exists(binarySavePath);
    }

    /// <summary>
    /// Checks whether a JSON save file exists for the specified save name.
    /// </summary>
    public static bool JsonSaveFileExists(string name)
    {
        return File.Exists(Path.Combine(
            Application.persistentDataPath,
            Path.GetFileNameWithoutExtension(name) + ".json"));
    }

    /// <summary>
    /// Checks whether a binary save file exists for the specified save name.
    /// </summary>
    public static bool BinarySaveFileExists(string name)
    {
        return File.Exists(Path.Combine(
            Application.persistentDataPath,
            Path.GetFileNameWithoutExtension(name) + ".dat"));
    }

    /// <summary>
    /// Loads game state from disk using either binary or JSON format.
    /// </summary>
    /// <returns>True if loading succeeds; otherwise false.</returns>
    public bool LoadGame()
    {
        Debug.Log("[LoadManager] LOADING...");
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        //TODO: Switch
        if (!SaveFileExists(jsonSavePath))
        {
            Debug.LogWarning($"[LoadManager] No save file found at: {jsonSavePath}");
            return false;
        }

        if (!SaveFileExists(binarySavePath))
        {
            Debug.LogWarning($"[LoadManager] No save file found at: {jsonSavePath}");
            return false;
        }

        return TryLoadBinary() || TryLoadJson();
    }

    /// <summary>
    /// Attempts to load game state from binary save file.
    /// </summary>
    /// <returns>True if successful; otherwise false.</returns>
    private bool TryLoadBinary()
    {
        Debug.Log("[LoadManager] Trying binary load...");

        if (!File.Exists(binarySavePath))
            return false;

        try
        {
            DtoGameData saveData = new DtoGameData();

            using FileStream stream = new FileStream(binarySavePath, FileMode.Open);
            using BinaryReader reader = new BinaryReader(stream);

            // MANAGED
            saveData.managed = new DtoManagedData
            {
                camPosition = ReadFloat3(reader),
                camRotation = ReadQuaternion(reader)
            };

            // RESOURCES
            int resCount = reader.ReadInt32();
            saveData.resources = new DtoResourceData
            {
                resources = new List<DtoResourceData.SaveResourceEntry>()
            };

            for (int i = 0; i < resCount; i++)
            {
                saveData.resources.resources.Add(new DtoResourceData.SaveResourceEntry
                {
                    resourceKey = new ResourceKey { name = reader.ReadString() },
                    amount = reader.ReadInt32()
                });
            }

            // BUILDINGS
            int buildingCount = reader.ReadInt32();
            saveData.buildings = new List<DtoBuildingData>();

            for (int i = 0; i < buildingCount; i++)
            {
                DtoBuildingData b = new DtoBuildingData
                {
                    prefabKey = reader.ReadString(),
                    position = ReadFloat3(reader),
                    rotation = ReadQuaternion(reader),
                    ownerID = reader.ReadInt32(),
                    factionID = reader.ReadUInt32(),
                    selected = reader.ReadBoolean(),
                    currentHealth = reader.ReadInt32()
                };

                bool hasTrainer = reader.ReadBoolean();

                if (hasTrainer)
                {
                    DtoTrainerData t = new DtoTrainerData
                    {
                        currentProgress = reader.ReadSingle(),
                        maxProgress = reader.ReadSingle(),
                        activeUnitKey = reader.ReadString(),
                        spawnPointOffset = new Float3Serializable(ReadFloat3(reader)),
                        rallyPositionOffset = new Float3Serializable(ReadFloat3(reader)),
                        onUnitQueueChange = reader.ReadBoolean(),
                        trainingQueue = new List<string>()
                    };

                    int qCount = reader.ReadInt32();
                    for (int q = 0; q < qCount; q++)
                        t.trainingQueue.Add(reader.ReadString());

                    b.trainerData = t;
                }

                saveData.buildings.Add(b);
            }

            // UNITS
            int unitCount = reader.ReadInt32();
            saveData.units = new List<DtoUnitData>();

            for (int i = 0; i < unitCount; i++)
            {
                saveData.units.Add(new DtoUnitData
                {
                    prefabKey = reader.ReadString(),
                    position = ReadFloat3(reader),
                    rotation = ReadQuaternion(reader),
                    ownerID = reader.ReadInt32(),
                    factionID = reader.ReadUInt32(),
                    selected = reader.ReadBoolean(),
                    requirePathing = reader.ReadBoolean(),
                    unitMoverPosition = ReadFloat3(reader),
                    targetPosition = ReadFloat3(reader),
                    postFormationPosition = ReadFloat3(reader),
                    lastMoveVector = ReadFloat3(reader),
                    targetEntity = new Entity
                    {
                        Index = reader.ReadInt32(),
                        Version = reader.ReadInt32()
                    },
                    currentHealth = reader.ReadInt32()
                });
            }

            OverwriteData(saveData);

            Debug.Log("[LoadManager] Binary load successful.");
            return true;
        }
        catch (EndOfStreamException e)
        {
            Debug.LogError($"[LoadManager] Binary file ended abruptly (corrupt or truncated save): {e.Message}");
            return false;
        }
        catch (IOException e)
        {
            Debug.LogError($"[LoadManager] Binary IO error (disk issue, locked file, or read failure): {e.Message}");
            return false;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.LogError($"[LoadManager] No permission to read binary save: {e.Message}");
            return false;
        }
        catch (FormatException e)
        {
            Debug.LogError($"[LoadManager] Binary format invalid (save structure mismatch): {e.Message}");
            return false;
        }
        catch (ObjectDisposedException e)
        {
            Debug.LogError($"[LoadManager] Binary stream was disposed unexpectedly: {e.Message}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoadManager] Unexpected binary load error: {e}");
            return false;
        }
    }

    /// <summary>
    /// Attempts to load game state from JSON save file.
    /// </summary>
    /// <returns>True if successful; otherwise false.</returns>
    private bool TryLoadJson()
    {
        try
        {
            string json = File.ReadAllText(jsonSavePath);
            DtoGameData saveData = JsonUtility.FromJson<DtoGameData>(json);

            if (saveData.units == null && saveData.buildings == null)
            {
                Debug.LogError("[LoadManager] JSON parsed but data is null or invalid.");
                return false;
            }

            OverwriteData(saveData);

            Debug.Log("[LoadManager] JSON load successful.");
            return true;
        }
        catch (FileNotFoundException e)
        {
            Debug.LogError($"[LoadManager] JSON file missing: {e.Message}");
            return false;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.LogError($"[LoadManager] No permission to read JSON: {e.Message}");
            return false;
        }
        catch (IOException e)
        {
            Debug.LogError($"[LoadManager] IO error reading JSON: {e.Message}");
            return false;
        }
        catch (ArgumentException e)
        {
            Debug.LogError($"[LoadManager] JSON path or content invalid: {e.Message}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoadManager] Unexpected JSON error: {e}");
            return false;
        }
    }

    /// <summary>
    /// Destroys all existing unit and building entities before loading new state.
    /// </summary>
    private void ClearPreviousEntities()
    {
        Debug.Log("[LoadManager] Clearing units and buildings...");

        EntityQuery unitQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Unit>()
            .Build(entityManager);

        using var unitArray = unitQuery.ToEntityArray(Allocator.Temp);
        foreach (var unitEntity in unitArray)
        {
            entityManager.DestroyEntity(unitEntity);
        }

        EntityQuery buildingQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Building>()
            .Build(entityManager);

        using var buildingArray = buildingQuery.ToEntityArray(Allocator.Temp);
        foreach (var buildingEntity in buildingArray)
        {
            entityManager.DestroyEntity(buildingEntity);
        }
    }

    /// <summary>
    /// Replaces current world state with loaded save data.
    /// </summary>
    /// <param name="save">Loaded game data.</param>
    private void OverwriteData(DtoGameData save)
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        ClearPreviousEntities();

        LoadManaged(save.managed);
        LoadResources(save.resources);
        LoadBuildings(save.buildings);
        LoadUnits(save.units);

        SelectionManager.Instance.TriggerOnSelectionChange();
    }

    /// <summary>
    /// Loads all units from save data.
    /// </summary>
    private void LoadUnits(List<DtoUnitData> units)
    {
        Debug.Log($"[LoadManager] Loading UNITS: {units.Count}");

        foreach (DtoUnitData unitData in units)
        {
            ConstructUnit(unitData);
        }
    }

    /// <summary>
    /// Constructs a unit ECS entity from serialized data.
    /// </summary>
    private void ConstructUnit(DtoUnitData unitData)
    {
        EntityPrefabKey entityPrefabKey = new EntityPrefabKey
        {
            name = unitData.prefabKey,
        };

        Entity prefabEntity = LookupEntityPrefab.FetchEntityPrefab(entityPrefabKey);
        Entity entity = entityManager.Instantiate(prefabEntity);

        LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entity);

        Unit unit = entityManager.GetComponentData<Unit>(entity);
        Faction faction = entityManager.GetComponentData<Faction>(entity);
        bool isSelected = unitData.selected;
        Selected selected = entityManager.GetComponentData<Selected>(entity);

        UnitMover unitMover = entityManager.GetComponentData<UnitMover>(entity);
        ManualMove manualMove = entityManager.GetComponentData<ManualMove>(entity);
        PathRequest pathRequest = entityManager.GetComponentData<PathRequest>(entity);
        FlowFieldFollower flowFieldFollower = entityManager.GetComponentData<FlowFieldFollower>(entity);

        Health health = entityManager.GetComponentData<Health>(entity);

        localTransform.Position = unitData.position;
        localTransform.Rotation = unitData.rotation;

        unit.ownerID = unitData.ownerID;
        faction.factionID = unitData.factionID;
        selected.onSelected = isSelected;

        unitMover.targetPosition = unitData.unitMoverPosition;
        unitMover.hasStartedTargetPosition = true;

        manualMove.targetPosition = unitData.targetPosition;
        manualMove.postFormationPosition = unitData.postFormationPosition;

        pathRequest.targetPosition = unitData.targetPosition;
        pathRequest.postFormationPosition = unitData.postFormationPosition;

        flowFieldFollower.lastMoveVector = unitData.lastMoveVector;

        health.currentHealth = unitData.currentHealth;

        entityManager.SetComponentData(entity, localTransform);
        entityManager.SetComponentData(entity, unit);
        entityManager.SetComponentData(entity, faction);
        entityManager.SetComponentEnabled<Selected>(entity, isSelected);
        entityManager.SetComponentData(entity, selected);
        entityManager.SetComponentData(entity, unitMover);
        entityManager.SetComponentData(entity, manualMove);
        entityManager.SetComponentData(entity, pathRequest);
        entityManager.SetComponentData(entity, flowFieldFollower);
        entityManager.SetComponentData(entity, health);

        entityManager.SetComponentEnabled<PathRequest>(entity, unitData.requirePathing);
    }

    /// <summary>
    /// Loads all buildings from save data.
    /// </summary>
    private void LoadBuildings(List<DtoBuildingData> buildings)
    {
        Debug.Log($"[LoadManager] Loading BUILDINGS: {buildings.Count}");

        foreach (DtoBuildingData buildingData in buildings)
        {
            ConstructBuilding(buildingData);
        }
    }

    /// <summary>
    /// Constructs a building ECS entity from serialized data.
    /// </summary>
    private void ConstructBuilding(DtoBuildingData buildingData)
    {
        EntityPrefabKey entityPrefabKey = new EntityPrefabKey
        {
            name = buildingData.prefabKey,
        };

        Entity prefabEntity = LookupEntityPrefab.FetchEntityPrefab(entityPrefabKey);
        Debug.Log($"[LoadManager] Fetching Building: {entityPrefabKey.name}");

        Entity entity = entityManager.Instantiate(prefabEntity);

        LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entity);

        Building building = entityManager.GetComponentData<Building>(entity);
        Faction faction = entityManager.GetComponentData<Faction>(entity);
        bool isSelected = buildingData.selected;
        Selected selected = entityManager.GetComponentData<Selected>(entity);
        Health health = entityManager.GetComponentData<Health>(entity);

        localTransform.Position = buildingData.position;
        localTransform.Rotation = buildingData.rotation;

        building.ownerID = buildingData.ownerID;
        faction.factionID = buildingData.factionID;
        selected.onSelected = isSelected;
        health.currentHealth = buildingData.currentHealth;

        entityManager.SetComponentData(entity, localTransform);
        entityManager.SetComponentData(entity, building);
        entityManager.SetComponentData(entity, faction);
        entityManager.SetComponentEnabled<Selected>(entity, isSelected);
        entityManager.SetComponentData(entity, selected);
        entityManager.SetComponentData(entity, health);

        if (entityManager.HasComponent<Trainer>(entity) &&
            entityManager.HasBuffer<QueuedUnitBuffer>(entity))
        {
            DynamicBuffer<QueuedUnitBuffer> unitQueueBuffer =
                entityManager.GetBuffer<QueuedUnitBuffer>(entity, isReadOnly: false);

            Trainer trainer = buildingData.trainerData.ToTrainer();
            buildingData.trainerData.RewriteQueuedUnitBuffer(unitQueueBuffer);

            entityManager.SetComponentData(entity, trainer);
        }
    }

    /// <summary>
    /// Loads resource values into the ResourceManager.
    /// </summary>
    private void LoadResources(DtoResourceData resources)
    {
        Debug.Log("[LoadManager] Loading RESOURCES...");

        ResourceManager.Instance.OverrideDict(resources.ToDictionary());
    }

    /// <summary>
    /// Restores non-ECS managed state such as camera transform.
    /// </summary>
    private void LoadManaged(DtoManagedData managed)
    {
        Debug.Log("[LoadManager] Loading MANAGED DATA...");

        cameraControllerGizmo.position = managed.camPosition;
        cameraControllerGizmo.rotation = managed.camRotation;
    }
}