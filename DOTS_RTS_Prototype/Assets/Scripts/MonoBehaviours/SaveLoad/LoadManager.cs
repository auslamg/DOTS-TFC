/* using System;
using System.IO;
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

    public static LoadManager Instance { get; private set; }

    private EntityManager entityManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    public bool SaveFileExists()
    {
        return File.Exists(savePath);
    }

    public SaveGameData LoadGame()
    {
        Debug.Log("[LoadManager] LOADING...");

        if (!File.Exists(savePath))
        {
            Debug.LogWarning($"[LoadManager] No save file found at: {savePath}");
            return default;
        }

        string json = File.ReadAllText(savePath);
        SaveGameData saveData = JsonUtility.FromJson<SaveGameData>(json);

        ApplySave(saveData);

        Debug.Log("[LoadManager] Load complete.");
        return saveData;
    }

    private void ApplySave(SaveGameData save)
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        ClearWorld();

        LoadManaged(save.managed);
        LoadResources(save.resources);
        LoadBuildings(save.buildings);
        LoadUnits(save.units);
    }

    // ------------------------------------------------------------
    // CLEAR EXISTING WORLD
    // ------------------------------------------------------------
    private void ClearWorld()
    {
        Debug.Log("[LoadManager] Clearing ECS world...");

        var allEntities = entityManager.GetAllEntities(Allocator.Temp);

        foreach (var e in allEntities)
        {
            entityManager.DestroyEntity(e);
        }

        allEntities.Dispose();
    }

    // ------------------------------------------------------------
    // UNITS
    // ------------------------------------------------------------
    private void LoadUnits(System.Collections.Generic.List<SaveUnitData> units)
    {
        Debug.Log($"[LoadManager] Loading UNITS: {units.Count}");

        foreach (var u in units)
        {
            // NOTE: you will likely replace this with your prefab system
            Entity entity = entityManager.CreateEntity();

            entityManager.AddComponentData(entity, new LocalTransform
            {
                Position = u.position,
                Rotation = u.rotation,
                Scale = 1f
            });

            entityManager.AddComponentData(entity, new Unit
            {
                ownerID = u.ownerID
            });

            entityManager.AddComponentData(entity, new Faction
            {
                factionID = u.factionID
            });

            entityManager.AddComponentData(entity, new Health
            {
                currentHealth = u.currentHealth
            });

            entityManager.AddComponentData(entity, new ManualMove
            {
                targetPosition = u.movePosition
            });

            entityManager.AddComponentData(entity, new ManualTarget
            {
                targetEntity = u.targetEntity
            });

            Debug.Log($"[LoadManager] Loaded unit: {u.prefabKey}");
        }
    }

    // ------------------------------------------------------------
    // BUILDINGS
    // ------------------------------------------------------------
    private void LoadBuildings(System.Collections.Generic.List<SaveBuildingData> buildings)
    {
        Debug.Log($"[LoadManager] Loading BUILDINGS: {buildings.Count}");

        foreach (var b in buildings)
        {
            Entity entity = entityManager.CreateEntity();

            entityManager.AddComponentData(entity, new LocalTransform
            {
                Position = b.position,
                Rotation = b.rotation,
                Scale = 1f
            });

            entityManager.AddComponentData(entity, new Building
            {
                ownerID = b.ownerID
            });

            entityManager.AddComponentData(entity, new Faction
            {
                factionID = b.factionID
            });

            entityManager.AddComponentData(entity, new Health
            {
                currentHealth = b.currentHealth
            });

            Debug.Log($"[LoadManager] Loaded building: {b.prefabKey}");
        }
    }
    
    private void LoadResources(SaveResourceData resources)
    {
        Debug.Log("[LoadManager] Loading RESOURCES...");

        ResourceManager.Instance.resourceAmountDictionary = resources.ToDictionary();
    }
    
    private void LoadManaged(SaveManagedData managed)
    {
        Debug.Log("[LoadManager] Loading MANAGED DATA...");

        cameraControllerGizmo.position = managed.camPosition;
        cameraControllerGizmo.rotation = managed.camRotation;
    }
} */