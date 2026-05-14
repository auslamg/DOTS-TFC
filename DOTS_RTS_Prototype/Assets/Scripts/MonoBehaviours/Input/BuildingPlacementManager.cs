using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using BoxCollider = UnityEngine.BoxCollider;
using Ray = UnityEngine.Ray;

/// <summary>
/// Manages player-driven building placement, including ghost preview rendering,
/// placement validation, and ECS entity instantiation.
/// </summary>
/// <remarks>
/// Handles grid snapping, collision validation, resource cost checks, and special rules
/// such as harvester-resource proximity requirements.
/// </remarks>
public class BuildingPlacementManager : MonoBehaviour
{
    /// <summary>
    /// Scriptable object defining the currently selected building type, prefab, and rules.
    /// </summary>
    [SerializeField]
    [Tooltip("Currently selected building definition used for ghost preview and placement rules.")]
    private BuildingDataSO buildingDataSO;

    /// <summary>
    /// ECS entity manager used for instantiating and configuring building entities.
    /// </summary>
    private EntityManager entityManager;

    /// <summary>
    /// Grid configuration used for snapping building placement to world coordinates.
    /// </summary>
    private GridData gridData;

    /// <summary>
    /// Current snapped placement position derived from mouse world position and grid rules.
    /// </summary>
    private Vector3 placePosition => GridUtil.SnapWorldPosition(mouseWorldPosition, gridData.gridCellSize);

    /// <summary>
    /// Gets or sets the active building type and updates the ghost preview accordingly.
    /// </summary>
    /// <remarks>
    /// Setting this value destroys the previous ghost instance and creates a new one
    /// if the assigned building is valid.
    /// </remarks>
    public BuildingDataSO activeBuildingDataSO
    {
        get => buildingDataSO;
        set
        {
            buildingDataSO = value;

            if (ghostPrefab != null)
            {
                Destroy(ghostPrefab.gameObject);
            }

            if (!buildingDataSO.IsNone())
            {
                ghostPrefab = Instantiate(buildingDataSO.buildingGhostPrefab);

                foreach (MeshRenderer mesh in ghostPrefab.GetComponentsInChildren<MeshRenderer>())
                {
                    mesh.material = GameAssets.Instance.validGhostMaterial;
                }
            }

            OnActiveBuildingDataChange?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Runtime ghost preview instance shown at the current placement position.
    /// </summary>
    [SerializeField]
    [Tooltip("Runtime ghost preview object shown while placing buildings.")]
    private GameObject ghostPrefab;

    /// <summary>
    /// Multiplier applied to collider bounds during placement validation.
    /// Higher values make placement stricter.
    /// </summary>
    [SerializeField]
    [Range(1, 3)]
    [Tooltip("Multiplier applied to collider extents when validating building overlap.")]
    private float placingExtentsOffset = 1.1f;

    /// <summary>
    /// Event triggered when the active building selection changes.
    /// </summary>
    public event EventHandler OnActiveBuildingDataChange;

    /// <summary>
    /// Current world-space mouse position from the input system.
    /// </summary>
    private Vector3 mouseWorldPosition => MouseWorldPosition.Instance.GetPosition();

    /// <summary>
    /// Singleton instance of the BuildingPlacementManager.
    /// </summary>
    public static BuildingPlacementManager Instance { get; private set; }

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
    /// Unity lifecycle method. Initializes singleton and ECS references.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    /// <summary>
    /// Assigns grid data used for placement snapping.
    /// </summary>
    /// <param name="gridData">Grid configuration data.</param>
    public void SetGridData(GridData gridData)
    {
        this.gridData = gridData;
    }

    /// <summary>
    /// Updates ghost preview position and handles placement input logic each frame.
    /// </summary>
    private void Update()
    {
        if (ghostPrefab != null)
        {
            ghostPrefab.transform.position = placePosition;

            if (CanPlaceBuilding() &&
                ResourceManager.Instance.CanSpendResourceValues(activeBuildingDataSO.constructionCost))
            {
                SetGhostColor(new Color(0, 0.5f, 1, 0.25f));
            }
            else if (CanPlaceBuilding())
            {
                SetGhostColor(new Color(1, 1, 0, 0.25f));
            }
            else
            {
                SetGhostColor(new Color(1, 0, 0, 0.25f));
            }
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (activeBuildingDataSO.IsNone())
        {
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (ResourceManager.Instance.CanSpendResourceValues(activeBuildingDataSO.constructionCost))
            {
                if (CanPlaceBuilding())
                {
                    EntityPrefabKey buildingKey = new EntityPrefabKey
                    {
                        name = activeBuildingDataSO.name
                    };

                    Debug.Log($"[BuildingPlacer] Placing buildings: {buildingKey.name}");

                    Entity spawnedEntity =
                        entityManager.Instantiate(DataLookup.FetchEntityPrefab(buildingKey));

                    entityManager.SetComponentData(
                        spawnedEntity,
                        LocalTransform.FromPosition(placePosition));

                    ResourceManager.Instance.SpendResourceValues(activeBuildingDataSO.constructionCost);
                    activeBuildingDataSO = GameAssets.Instance.buildingDataRegistrySO.none;
                }
            }
            else
            {
                Debug.Log("[BuildingPlacer] Insufficient funds.");
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            activeBuildingDataSO = GameAssets.Instance.buildingDataRegistrySO.none;
        }
    }

    /// <summary>
    /// Validates whether the current placement position is valid for building placement.
    /// </summary>
    /// <returns>True if placement is allowed; otherwise false.</returns>
    /// <remarks>
    /// Checks for collisions, spacing constraints, and special rules such as harvester resource proximity.
    /// </remarks>
    private bool CanPlaceBuilding()
    {
        if (buildingDataSO.IsNone())
        {
            return false;
        }

        CollisionWorld collisionWorld = entityManager.GetCollisionWorld();

        CollisionFilter buildingsFilter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith =
                1u << GameAssets.BUILDINGS_LAYER |
                1u << GameAssets.UNITS_LAYER |
                1u << GameAssets.OBSTRUCTION_LAYER,
            GroupIndex = 0
        };

        BoxCollider boxCollider = buildingDataSO.prefabGO.GetComponent<BoxCollider>();
        float colliderOffsetMultiplier = placingExtentsOffset >= 1 ? placingExtentsOffset : 1;

        NativeList<DistanceHit> hitList = new NativeList<DistanceHit>(Allocator.Temp);

        if (collisionWorld.OverlapBox(
                center: placePosition,
                orientation: Quaternion.identity,
                boxCollider.size / 2 * colliderOffsetMultiplier,
                ref hitList,
                buildingsFilter))
        {
            return false;
        }

        hitList.Clear();

        if (collisionWorld.OverlapSphere(
                position: placePosition,
                radius: buildingDataSO.minDistanceToSimilar,
                ref hitList,
                buildingsFilter))
        {
            foreach (DistanceHit distanceHit in hitList)
            {
                if (entityManager.HasComponent<BuildingDataSOHolder>(distanceHit.Entity))
                {
                    BuildingDataSOHolder buildingData =
                        entityManager.GetComponentData<BuildingDataSOHolder>(distanceHit.Entity);

                    if (buildingDataSO.buildingType == buildingData.buildingKeyType)
                    {
                        return false;
                    }
                }
            }
        }

        CollisionFilter resourceSourcesFilter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = 1u << GameAssets.RESOURCE_SOURCES_LAYER,
            GroupIndex = 0
        };

        if (buildingDataSO.buildingType == BuildingType.Harvester)
        {
            bool validResource = false;

            Entity harvesterEntity =
                LookupEntityPrefab.FetchEntityPrefab(EntityPrefabKey.From(buildingDataSO.buildingKey));

            Harvester harvester = entityManager.GetComponentData<Harvester>(harvesterEntity);

            if (collisionWorld.OverlapSphere(
                    position: placePosition,
                    radius: harvester.harvestingRange,
                    ref hitList,
                    resourceSourcesFilter))
            {
                foreach (DistanceHit distanceHit in hitList)
                {
                    if (entityManager.HasComponent<ResourceSource>(distanceHit.Entity))
                    {
                        ResourceSource resourceSource =
                            entityManager.GetComponentData<ResourceSource>(distanceHit.Entity);

                        if (harvester.harvestedResourceKey == resourceSource.generatedResourceKey)
                        {
                            validResource = true;
                            break;
                        }
                    }
                }
            }

            return validResource;
        }

        return true;
    }

    /// <summary>
    /// Applies a color tint to the ghost preview to indicate placement validity.
    /// </summary>
    /// <param name="color">Target ghost preview color.</param>
    private void SetGhostColor(Color color)
    {
        foreach (MeshRenderer mesh in ghostPrefab.GetComponentsInChildren<MeshRenderer>())
        {
            mesh.material.color = color;
        }
    }
}