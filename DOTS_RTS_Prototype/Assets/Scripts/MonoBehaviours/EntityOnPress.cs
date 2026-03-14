using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

public class EntityOnPress : MonoBehaviour
{
    public static EntityOnPress Instance { get; private set; }
    [SerializeField] private string entity;

    /// <summary>
    /// Awake() : MonoBehaviour
    /// Used for singleton logic.
    /// </summary>
    void Awake()
    {
        //Singleton logic
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


    // Update is called once per frame
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
