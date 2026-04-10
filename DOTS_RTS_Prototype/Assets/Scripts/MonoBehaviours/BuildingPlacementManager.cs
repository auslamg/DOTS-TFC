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

    public BuildingDataSO ActiveBuildingDataSO
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
    /// Raised when <see cref="ActiveBuildingDataSO"/> changes.
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
    void Awake()
    {
        // Initialize singleton instance state.
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
    /// Updates building ghost position and handles left/right-click placement controls.
    /// </summary>
    void Update()
    {
        if (ghostPrefab != null)
        {
            ghostPrefab.transform.position = mouseWorldPosition;
        }
        // Ignore placement clicks while the cursor is interacting with UI.
        if (EventSystem.current.IsPointerOverGameObject())
        {

            return;
        }

        if (ActiveBuildingDataSO.IsNone())
        {

            return;
        }



        if (Input.GetMouseButtonDown(0))
        {
            if (CanPlaceBuilding())
            {
                EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

                // Create the prefab lookup key expected by the ECS data layer.
                // Retrieve key from active buildingData
                EntityPrefabKey buildingKey = new EntityPrefabKey
                {
                    name = ActiveBuildingDataSO.name
                };

                /* Debug.Log($"Placing buildings: {buildingKey.name}"); */

                Entity spawnedEntity = entityManager.Instantiate(DataLookup.FetchEntityPrefab(buildingKey));
                entityManager.SetComponentData(spawnedEntity, LocalTransform.FromPosition(mouseWorldPosition));
            }
            else
            {
                /* Debug.Log($"Cannot place building: {buildingKey} at {mouseWorldPosition}"); */
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            //On left click set no active building
            ActiveBuildingDataSO = GameAssets.Instance.buildingDataRegistrySO.none;
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
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        EntityQuery entityQuery = entityManager.CreateEntityQuery(typeof(PhysicsWorldSingleton));
        CollisionWorld collisionWorld = entityManager.GetCollisionWorld();

        CollisionFilter filter = new CollisionFilter
        {
            BelongsTo = ~0u, //All layers
            CollidesWith = 1u << GameAssets.BUILDINGS_LAYER,
            GroupIndex = 0
        };

        BoxCollider boxCollider = buildingDataSO.prefabGO.GetComponent<BoxCollider>();
        float colliderOffsetMultiplier = placingExtentsOffset >= 1 ? placingExtentsOffset : 1;
        NativeList<DistanceHit> hitList = new NativeList<DistanceHit>(Allocator.Temp);

        // Reject the placement if the building footprint overlaps any existing building.
        if (collisionWorld.OverlapBox(
                center: mouseWorldPosition,
                orientation: Quaternion.identity,
                boxCollider.size / 2 * colliderOffsetMultiplier,
                ref hitList,
                filter))
        {
            return false;
        }

        hitList.Clear();

        // Enforce minimum spacing between buildings of the same type.
        if (collisionWorld.OverlapSphere(
                position: mouseWorldPosition,
                radius: buildingDataSO.minDistanceToSimilar,
                ref hitList,
                filter))
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
        return true;
    }
}
