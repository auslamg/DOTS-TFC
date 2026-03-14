using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingPlacementManager : MonoBehaviour
{
    public static BuildingPlacementManager Instance { get; private set; }
    [SerializeField] private string buildingKey;

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
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mouseWorldPosition = MouseWorldPosition.Instance.GetPosition();
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            EntityPrefabKey buildingKey = new EntityPrefabKey
            {
                name = this.buildingKey
            };
            
            Debug.Log($"Placing buildings: {buildingKey.name}");

            Entity spawnedEntity = entityManager.Instantiate(DataLookup.FetchEntityPrefab(buildingKey));
            entityManager.SetComponentData(spawnedEntity, LocalTransform.FromPosition(mouseWorldPosition));
        }

    }
}
