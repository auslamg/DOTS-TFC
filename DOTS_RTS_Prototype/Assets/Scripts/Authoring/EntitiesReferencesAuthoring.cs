using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Managed component for the <see cref="EntitiesReferences"/> unmanaged component.
/// </summary>
/// <remarks>
/// The component is a Singleton.
/// </remarks>
class EntitiesReferencesAuthoring : MonoBehaviour
{
    public GameObject bulletPrefabGameObject;
    public GameObject enemyPrefabGameObject;
    public GameObject shootLightPrefabGameObject;
    public GameObject scoutPrefabGameObject;
    public GameObject soldierPrefabGameObject;

    public GameObject[] gameObjectPrefabs;

    public static EntitiesReferencesAuthoring Instance { get; private set; }

    /// <summary>
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

}

/// <summary>
/// Baker for the <see cref="EntitiesReferences"/> unmanaged component.
/// </summary>
class EntitiesReferencesBaker : Baker<EntitiesReferencesAuthoring>
{
    private Entity GetPrefabEntity(GameObject prefabGo)
    {
        return GetEntity(prefabGo, TransformUsageFlags.None);
    }

    public override void Bake(EntitiesReferencesAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        //Smart initizalize to avoid null reference exceptions in case the user forgets to set the array in the inspector. 
        GameObject[] prefabsArray = authoring.gameObjectPrefabs ?? Array.Empty<GameObject>();
        //Sort items for binary search optimization.
        Array.Sort(prefabsArray, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

        //Ensure prefab names are unique to avoid issues with the registry.
        HashSet<string> usedPrefabNames = new HashSet<string>(StringComparer.Ordinal);

        DynamicBuffer<EntityReference> entityRefsBuffer = AddBuffer<EntityReference>(entity);
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
            entityRefsBuffer.Add(new EntityReference
            {
                entityKey = new EntityReferenceKey
                {
                    name = entityPrefab.name,
                },
                prefabEntity = prefabEntity
            });
        }

        AddComponent(entity, new EntitiesReferences
        {
            bulletPrefabEntity = GetPrefabEntity(authoring.bulletPrefabGameObject),
            enemyPrefabEntity = GetPrefabEntity(authoring.enemyPrefabGameObject),
            shootLightPrefabEntity = GetPrefabEntity(authoring.shootLightPrefabGameObject),
            scoutPrefabEntity = GetPrefabEntity(authoring.scoutPrefabGameObject),
            soldierPrefabEntity = GetPrefabEntity(authoring.soldierPrefabGameObject)
        });
    }
}

/// <summary>
/// Used for passing down references to entity prefabs.
/// Must set values for all fields on instantiation.
/// </summary>
/// <remarks>
/// The component is a Singleton, which should be obtained through <see cref="SystemAPI.GetSingleton()"/>.
/// </remarks>
public struct EntitiesReferences : IComponentData
{
    public Entity bulletPrefabEntity;
    public Entity enemyPrefabEntity;
    public Entity shootLightPrefabEntity;
    public Entity scoutPrefabEntity;
    public Entity soldierPrefabEntity;
}

public struct EntityReference
    : IBufferElementData
{
    public EntityReferenceKey entityKey;
    public Entity prefabEntity;
}

/// <summary>
/// Unique identifier for a <see cref="EntityReference"/> struct, obtained from the prefab name.
/// </summary>
public struct EntityReferenceKey : IEquatable<EntityReferenceKey>, IComparable<EntityReferenceKey>, IComparable<IEntityPrefabMappable>
{
    public FixedString64Bytes name;
    public bool Equals(EntityReferenceKey other)
    {
        return name.Equals(other.name);
    }
    public override bool Equals(object obj)
    {
        return obj is EntityReferenceKey other && Equals(other);
    }
    public int CompareTo(EntityReferenceKey other)
    {
        int cmp = name.CompareTo(other.name);
        return cmp;
    }

    public int CompareTo(IEntityPrefabMappable other)
    {
        int cmp = name.CompareTo(other.GetKey());
        return cmp;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + name.GetHashCode();
            return hash;
        }
    }

    public static bool operator ==(EntityReferenceKey left, EntityReferenceKey right) => left.Equals(right);
    public static bool operator !=(EntityReferenceKey left, EntityReferenceKey right) => !left.Equals(right);
    public override string ToString()
    {
        return $"{name}";
    }
}

public interface IEntityPrefabMappable
{
    FixedString64Bytes GetKey();
}
