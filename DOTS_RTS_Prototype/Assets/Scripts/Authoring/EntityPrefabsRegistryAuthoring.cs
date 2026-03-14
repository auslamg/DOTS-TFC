using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Managed component for the <see cref="EntityPrefabsRegistry"/> unmanaged component.
/// </summary>
/// <remarks>
/// The component is a Singleton.
/// </remarks>
class EntityPrefabsRegistryAuthoring : MonoBehaviour
{
    public PrefabRegistrySO prefabRegistrySO;

    public static EntityPrefabsRegistryAuthoring Instance { get; private set; }

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

            Debug.Log($"EntitiesReferencesBaker: Added Entity Prefab '{entityPrefab.name}' .");

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
/// Used for passing down references to entity prefabs.
/// Must set values for all fields on instantiation.
/// </summary>
/// <remarks>
/// The component is a Singleton, which should be obtained through <see cref="SystemAPI.GetSingleton()"/>.
/// </remarks>
public struct EntityPrefabsRegistry : IComponentData
{
    public int holder;
}

public struct EntityPrefab : IBufferElementData
{
    public EntityPrefabKey entityRefKey;
    public Entity prefabEntity;
}

/// <summary>
/// Unique identifier for a <see cref="EntityPrefab"/> struct, obtained from the prefab name.
/// </summary>
public struct EntityPrefabKey : IEquatable<EntityPrefabKey>, IComparable<EntityPrefabKey>, IComparable<IEntityPrefabMappable>
{
    public FixedString64Bytes name;
    public bool Equals(EntityPrefabKey other)
    {
        return name.Equals(other.name);
    }
    public override bool Equals(object obj)
    {
        return obj is EntityPrefabKey other && Equals(other);
    }
    public int CompareTo(EntityPrefabKey other)
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

    public static bool operator ==(EntityPrefabKey left, EntityPrefabKey right) => left.Equals(right);
    public static bool operator !=(EntityPrefabKey left, EntityPrefabKey right) => !left.Equals(right);
    public override string ToString()
    {
        return $"{name}";
    }
}

public interface IEntityPrefabMappable
{
    FixedString64Bytes GetKey();
}
