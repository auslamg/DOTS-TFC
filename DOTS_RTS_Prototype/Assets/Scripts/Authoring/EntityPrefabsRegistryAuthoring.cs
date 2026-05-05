using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System.Linq;

/// <summary>
/// Authoring component that bakes data into <see cref="EntityPrefabsRegistry"/>.
/// </summary>
/// <remarks>
/// Behaves as a scene singleton.
/// </remarks>
class EntityPrefabsRegistryAuthoring : MonoBehaviour
{
    /// <summary>
    /// Source scriptable object containing the prefab list used to build the registry.
    /// </summary>
    [SerializeField]
    [Tooltip("Source scriptable object containing prefab GameObjects for the entity prefab registry.")]
    public PrefabRegistrySO prefabRegistrySO;

    /// <summary>
    /// Scene singleton instance for managed-side access.
    /// </summary>
    public static EntityPrefabsRegistryAuthoring Instance { get; private set; }

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

}

/// <summary>
/// Baker for the <see cref="EntityPrefabsRegistry"/> unmanaged component.
/// </summary>
class EntityPrefabsRegistryBaker : Baker<EntityPrefabsRegistryAuthoring>
{
    private Entity GetPrefabEntity(GameObject prefabGo)
    {
        return GetEntity(prefabGo, TransformUsageFlags.None);
    }

    public override void Bake(EntityPrefabsRegistryAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        if (authoring.prefabRegistrySO.VerifyConstruction())
        {
            Debug.Log($"Baking prefab entries into entity references: {authoring.prefabRegistrySO.entityReferenceKeySet.Count}");
        }

        //Sort items for binary search optimization
        GameObject[] prefabsArray = authoring.prefabRegistrySO.prefabGOs
            .OrderBy((GameObject go) => go.name)
            .ToArray();

        //Ensure prefab names are unique to avoid issues with the registry.
        HashSet<string> usedPrefabNames = new HashSet<string>(StringComparer.Ordinal);

        DynamicBuffer<EntityPrefab> entityRefsBuffer = AddBuffer<EntityPrefab>(entity);
        entityRefsBuffer.EnsureCapacity(prefabsArray.Length);

        //Add each prefab in the serialized list to the buffer.
        for (int entityIndex = 0; entityIndex < prefabsArray.Length; entityIndex++)
        {
            // Check for bake errors (null)
            GameObject entityPrefab = prefabsArray[entityIndex];
            if (entityPrefab == null)
            {
                Debug.LogError($"EntitiesReferencesBaker: Null prefab at index {entityIndex} in gameObjectPrefabs.");
                continue;
            }

            // Check for duplicates
            if (!usedPrefabNames.Add(entityPrefab.name))
            {
                Debug.LogError($"EntitiesReferencesBaker: Duplicate prefab name '{entityPrefab.name}' in gameObjectPrefabs. Keys must be unique.");
                continue;
            }

            // Add prefab reference to buffer
            Entity prefabEntity = GetPrefabEntity(entityPrefab);
            entityRefsBuffer.Add(new EntityPrefab
            {
                entityRefKey = new EntityPrefabKey
                {
                    name = entityPrefab.name,
                },
                prefabEntity = prefabEntity
            });
        }

        AddComponent(entity, new EntityPrefabsRegistry
        {
            holder = 1
        });
    }
}

/// <summary>
/// Singleton component that provides access to baked entity prefab references.
/// </summary>
/// <remarks>
/// Access this component through <see cref="SystemAPI.GetSingleton()"/>.
/// </remarks>
public struct EntityPrefabsRegistry : IComponentData
{
    /// <summary>
    /// Placeholder value used as singleton marker data.
    /// </summary>
    public int holder;
}

/// <summary>
/// Buffer entry that maps a prefab key to its baked prefab entity.
/// </summary>
public struct EntityPrefab : IBufferElementData
{
    /// <summary>
    /// Key used to look up the prefab entity.
    /// </summary>
    public EntityPrefabKey entityRefKey;
    /// <summary>
    /// Baked prefab entity reference.
    /// </summary>
    public Entity prefabEntity;
}
