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

public class LoadManager : MonoBehaviour
{
    [Header("Save path settings")]
    [SerializeField]
    private string fileName;

    private string jsonSavePath =>
    Path.Combine(
        Application.persistentDataPath,
        Path.GetFileNameWithoutExtension(fileName) + ".json");
    private string binarySavePath =>
    Path.Combine(
        Application.persistentDataPath,
        Path.GetFileNameWithoutExtension(fileName) + ".dat");

    [Header("Load file type: true for JSON, false for .dat.")]
    [SerializeField]
    private bool isJson;

    [Header("References")]
    /// <summary>
    /// Camera controller gizmo for camera position storage.
    /// </summary>
    [SerializeField]
    [Tooltip("Camera controller gizmo for camera position storage.")]
    private Transform cameraControllerGizmo;

    private EntityManager entityManager;

    [Header("References")]

    /// <summary>
    /// Global singleton access to the DOTS event bridge.
    /// </summary>
    public static LoadManager Instance { get; private set; }

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

    public bool SaveFileExists(string path)
    {
        return isJson ? File.Exists(jsonSavePath) : File.Exists(binarySavePath);
    }

    public static bool JsonSaveFileExists(string name)
    {
        return File.Exists(Path.Combine(
            Application.persistentDataPath,
            Path.GetFileNameWithoutExtension(name) + ".json"));
    }

    public static bool BinarySaveFileExists(string name)
    {
        return File.Exists(Path.Combine(
            Application.persistentDataPath,
            Path.GetFileNameWithoutExtension(name) + ".dat"));
    }

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

        /* if (TryLoadBinary())
            return true; */
        return TryLoadBinary() || TryLoadJson();
    }

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

    private void ClearPreviousEntities()
    {
        Debug.Log("[LoadManager] Clearing units and buildings...");

        EntityQuery unitQuery = new EntityQueryBuilder(Allocator.Temp).
            WithAll<Unit>().
            Build(entityManager);

        using var unitArray = unitQuery.ToEntityArray(Allocator.Temp);
        foreach (var unitEntity in unitArray)
        {
            entityManager.DestroyEntity(unitEntity);
        }

        EntityQuery buildingQuery = new EntityQueryBuilder(Allocator.Temp).
            WithAll<Building>().
            Build(entityManager);

        using var buildingArray = buildingQuery.ToEntityArray(Allocator.Temp);
        foreach (var buildingEntity in buildingArray)
        {
            entityManager.DestroyEntity(buildingEntity);
        }
    }

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

    private void LoadUnits(List<DtoUnitData> units)
    {
        Debug.Log($"[LoadManager] Loading UNITS: {units.Count}");

        foreach (DtoUnitData unitData in units)
        {
            ConstructUnit(unitData);
        }
    }

    private void ConstructUnit(DtoUnitData unitData)
    {
        // Fetch prefab.
        EntityPrefabKey entityPrefabKey = new EntityPrefabKey
        {
            name = unitData.prefabKey,
        };
        Entity prefabEntity = LookupEntityPrefab.FetchEntityPrefab(entityPrefabKey);

        // Rebuild the entity.
        Entity entity = entityManager.Instantiate(prefabEntity);

        // Save post-write data.
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

        // Value assignments.
        {
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
        }

        // Copy values.
        {
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
    }

    private void LoadBuildings(List<DtoBuildingData> buildings)
    {
        Debug.Log($"[LoadManager] Loading BUILDINGS: {buildings.Count}");

        foreach (DtoBuildingData buildingData in buildings)
        {
            ConstructBuilding(buildingData);
        }
    }

    private void ConstructBuilding(DtoBuildingData buildingData)
    {
        // Fetch prefab.
        EntityPrefabKey entityPrefabKey = new EntityPrefabKey
        {
            name = buildingData.prefabKey,
        };
        Entity prefabEntity = LookupEntityPrefab.FetchEntityPrefab(entityPrefabKey);
        Debug.Log($"[LoadManager] Fetching Building: {entityPrefabKey.name}");

        // Rebuild the entity.
        Entity entity = entityManager.Instantiate(prefabEntity);

        // Save post-write data.
        {
            LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entity);

            Building building = entityManager.GetComponentData<Building>(entity);
            Faction faction = entityManager.GetComponentData<Faction>(entity);
            bool isSelected = buildingData.selected;
            Selected selected = entityManager.GetComponentData<Selected>(entity);

            Health health = entityManager.GetComponentData<Health>(entity);

            // Value assignments.
            {
                localTransform.Position = buildingData.position;
                localTransform.Rotation = buildingData.rotation;

                building.ownerID = buildingData.ownerID;
                faction.factionID = buildingData.factionID;
                selected.onSelected = isSelected;

                health.currentHealth = buildingData.currentHealth;
            }

            // Copy values.
            {
                entityManager.SetComponentData(entity, localTransform);

                entityManager.SetComponentData(entity, building);
                entityManager.SetComponentData(entity, faction);
                entityManager.SetComponentEnabled<Selected>(entity, isSelected);
                entityManager.SetComponentData(entity, selected);

                entityManager.SetComponentData(entity, health);
            }
        }

        // Read trainer buffer data if necessary.
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

    private void LoadResources(DtoResourceData resources)
    {
        Debug.Log("[LoadManager] Loading RESOURCES...");

        ResourceManager.Instance.OverrideDict(resources.ToDictionary());
    }

    private void LoadManaged(DtoManagedData managed)
    {
        Debug.Log("[LoadManager] Loading MANAGED DATA...");

        cameraControllerGizmo.position = managed.camPosition;
        cameraControllerGizmo.rotation = managed.camRotation;
    }
}