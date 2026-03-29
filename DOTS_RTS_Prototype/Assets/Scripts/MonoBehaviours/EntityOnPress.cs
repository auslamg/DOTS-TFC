using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Debug utility that spawns an ECS prefab at mouse position when a key is pressed.
/// </summary>
/// <remarks>
/// Intended for quick in-editor testing. Press <c>E</c> to spawn the prefab
/// resolved from <see cref="entity"/> through the entity prefab registry.
/// </remarks>
public class EntityOnPress : MonoBehaviour
{
    /// <summary>
    /// Global singleton access to this debug spawner.
    /// </summary>
    public static EntityOnPress Instance { get; private set; }

    /// <summary>
    /// Prefab key used to resolve which entity to spawn on key press.
    /// </summary>
    [SerializeField]
    [Tooltip("Entity prefab key to spawn when pressing E.")]
    private string entity;

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
    /// Handles spawn input and performs spawn-at-mouse-position behavior.
    /// </summary>
    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            Vector3 mouseWorldPosition = MouseWorldPosition.Instance.GetPosition();
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            EntityPrefabKey entity = new EntityPrefabKey
            {
                name = this.entity
            };

            Entity spawnedEntity = entityManager.Instantiate(DataLookup.FetchEntityPrefab(entity));
            entityManager.SetComponentData(spawnedEntity, LocalTransform.FromPosition(mouseWorldPosition));
        }

    }
}
