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
/// Manages player-driven building placement by validating the target position
/// against existing structures and spawning the selected building entity.
/// </summary>
public class BuildingPlacementManager : MonoBehaviour
{
    /// <summary>
    /// Scriptable object containing prefab and placement rules for the current building type.
    /// </summary>
    [SerializeField]
    [Tooltip("Currently selected building definition used for ghost preview and placement rules.")]
    private BuildingDataSO buildingDataSO;

    private EntityManager entityManager;
    private GridData gridData;

    Vector3 placePosition => GridUtil.SnapWorldPosition(mouseWorldPosition, gridData.gridCellSize);

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
    /// Runtime ghost preview object displayed at the current mouse world position.
    /// </summary>
    [SerializeField]
    [Tooltip("Runtime ghost preview object shown while placing buildings.")]
    private GameObject ghostPrefab;

    /// <summary>
    /// Multiplier applied to the building collider extents when checking for overlaps.
    /// Higher values make placement validation more conservative.
    /// </summary>
    [SerializeField]
    [Range(1, 3)]
    [Tooltip("Multiplier applied to collider extents when validating building overlap.")]
    private float placingExtentsOffset = 1.1f;

    /// <summary>
    /// Raised when <see cref="activeBuildingDataSO"/> changes.
    /// </summary>
    public event EventHandler OnActiveBuildingDataChange;

    /// <summary>
    /// Retrieves the current mouse position projected into world space.
    /// </summary>
    private Vector3 mouseWorldPosition => MouseWorldPosition.Instance.GetPosition();

    /// <summary>
    /// Global access point for the active building placement manager.
    /// </summary>
    public static BuildingPlacementManager Instance { get; private set; }

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
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    public void SetGridData(GridData gridData)
    {
        this.gridData = gridData;
    }

    /// <summary>
    /// Updates building ghost position and handles left/right-click placement controls.
    /// </summary>
    void Update()
    {
        if (ghostPrefab != null)
        {
            ghostPrefab.transform.position = placePosition;

            if (CanPlaceBuilding())
            {
                SetGhostColor(true);
            }
            else
            {
                SetGhostColor(false);
            }
        }
        // Ignore placement clicks while the cursor is interacting with UI.
        if (EventSystem.current.IsPointerOverGameObject())
        {

            return;
        }

        if (activeBuildingDataSO.IsNone())
        {

            return;
        }



        if (Input.GetMouseButtonDown(0))
        {
            if (ResourceManager.Instance.CanSpendResourceValues(activeBuildingDataSO.constructionCost))
            {
                if (CanPlaceBuilding())
                {
                    // Create the prefab lookup key expected by the ECS data layer.
                    // Retrieve key from active buildingData
                    EntityPrefabKey buildingKey = new EntityPrefabKey
                    {
                        name = activeBuildingDataSO.name
                    };

                    Debug.Log($"Placing buildings: {buildingKey.name}");

                    Entity spawnedEntity = entityManager.Instantiate(DataLookup.FetchEntityPrefab(buildingKey));
                    entityManager.SetComponentData(spawnedEntity, LocalTransform.FromPosition(placePosition));

                    ResourceManager.Instance.SpendResourceValues(activeBuildingDataSO.constructionCost);
                    activeBuildingDataSO = GameAssets.Instance.buildingDataRegistrySO.none;
                }
                else
                {
                    /* Debug.Log($"Cannot place building: {buildingKey} at {mouseWorldPosition}"); */
                }
            }
            else
            {
                Debug.Log("Insufficient funds.");
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            //On left click set no active building
            activeBuildingDataSO = GameAssets.Instance.buildingDataRegistrySO.none;
        }

    }

    /// <summary>
    /// Checks whether a building can be placed at the current mouse position.
    /// Placement fails if the building would overlap any structure or if it is too close
    /// to another building of the same type.
    /// </summary>
    /// <returns>
    /// <c>true</c> when placement is valid; otherwise, <c>false</c>.
    /// </returns>
    private bool CanPlaceBuilding()
    {
        if (buildingDataSO.IsNone())
        {
            return false;
        }
        CollisionWorld collisionWorld = entityManager.GetCollisionWorld();

        CollisionFilter buildingsFilter = new CollisionFilter
        {
            BelongsTo = ~0u, //All layers
            CollidesWith = 1u << GameAssets.BUILDINGS_LAYER | 1u << GameAssets.UNITS_LAYER | 1u << GameAssets.OBSTRUCTION_LAYER,
            GroupIndex = 0
        };

        BoxCollider boxCollider = buildingDataSO.prefabGO.GetComponent<BoxCollider>();
        float colliderOffsetMultiplier = placingExtentsOffset >= 1 ? placingExtentsOffset : 1;
        NativeList<DistanceHit> hitList = new NativeList<DistanceHit>(Allocator.Temp);

        // Reject the placement if the building footprint overlaps any existing building.
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

        // Enforce minimum spacing between buildings of the same type.
        if (collisionWorld.OverlapSphere(
                position: placePosition,
                radius: buildingDataSO.minDistanceToSimilar,
                ref hitList,
                buildingsFilter))
        {
            foreach (DistanceHit distanceHit in hitList)
            {
                Debug.Log($"Checked entity at radius: {buildingDataSO.minDistanceToSimilar}");
                if (entityManager.HasComponent<BuildingDataSOHolder>(distanceHit.Entity))
                {
                    BuildingDataSOHolder buildingData = entityManager.GetComponentData<BuildingDataSOHolder>(distanceHit.Entity);
                    if (buildingDataSO.buildingType == buildingData.buildingKeyType)
                    {
                        return false;
                    }
                }
            }
        }


        CollisionFilter resourceSourcesFilter = new CollisionFilter
        {
            BelongsTo = ~0u, //All layers
            CollidesWith = 1u << GameAssets.RESOURCE_SOURCES_LAYER,
            GroupIndex = 0
        };

        // Enforce resource sources in range for harvesters.
        if (buildingDataSO.buildingType == BuildingType.Harvester)
        {
            bool validResource = false;
            Debug.Log("Checking for harvester proximity");
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
                    Debug.Log($"{distanceHit.Entity}");
                    if (entityManager.HasComponent<ResourceSource>(distanceHit.Entity))
                    {
                        Debug.Log("Checking resource compat");
                        ResourceSource resourceSource = entityManager.GetComponentData<ResourceSource>(distanceHit.Entity);
                        if (harvester.harvestedResourceKey == resourceSource.generatedResourceKey)
                        {
                            Debug.Log("Found harvester resource");
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

    private void SetGhostColor(bool placeable)
    {
        foreach (MeshRenderer mesh in ghostPrefab.GetComponentsInChildren<MeshRenderer>())
        {
            mesh.material.color = placeable ?
                new Color(0, 0.5f, 1, 0.25f) :
                new Color(1, 0, 0, 0.25f);
        }
    }
}
