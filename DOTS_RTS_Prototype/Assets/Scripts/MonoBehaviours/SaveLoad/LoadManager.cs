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

public class LoadManager : MonoBehaviour
{
    [Header("Save path settings")]
    [SerializeField]
    private string fileName;

    private string savePath => Path.Combine(Application.persistentDataPath, fileName);

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

    public bool SaveFileExists()
    {
        return File.Exists(savePath);
    }

    public bool LoadGame()
    {
        Debug.Log("[LoadManager] LOADING...");

        if (!File.Exists(savePath))
        {
            Debug.LogWarning($"[LoadManager] No save file found at: {savePath}");
            return false;
        }

        string json = File.ReadAllText(savePath);
        DtoGameData saveData = JsonUtility.FromJson<DtoGameData>(json);

        OverwriteData(saveData);
        UnitSelectionManager.Instance.TriggerOnSelectionChange();

        Debug.Log("[LoadManager] Load complete.");
        return true;
    }

    private void OverwriteData(DtoGameData save)
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        ClearPreviousEntities();

        LoadManaged(save.managed);
        LoadResources(save.resources);
        LoadBuildings(save.buildings);
        LoadUnits(save.units);
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
        bool selected = unitData.selected;

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
            entityManager.SetComponentEnabled<Selected>(entity, selected);

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
        Debug.Log($"Fetching Building: {entityPrefabKey.name}");

        // Rebuild the entity.
        Entity entity = entityManager.Instantiate(prefabEntity);

        // Save post-write data.
        {
            LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(entity);
            Building building = entityManager.GetComponentData<Building>(entity);
            Faction faction = entityManager.GetComponentData<Faction>(entity);
            bool selected = buildingData.selected;
            Health health = entityManager.GetComponentData<Health>(entity);

            // Value assignments.
            {
                localTransform.Position = buildingData.position;
                localTransform.Rotation = buildingData.rotation;
                building.ownerID = buildingData.ownerID;
                faction.factionID = buildingData.factionID;
                health.currentHealth = buildingData.currentHealth;
            }

            // Copy values.
            {
                entityManager.SetComponentData(entity, localTransform);
                entityManager.SetComponentData(entity, building);
                entityManager.SetComponentData(entity, faction);
                entityManager.SetComponentEnabled<Selected>(entity, selected);
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