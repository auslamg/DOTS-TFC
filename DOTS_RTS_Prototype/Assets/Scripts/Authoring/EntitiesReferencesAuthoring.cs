using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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
    public override void Bake(EntitiesReferencesAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);

        //Sort items for binary search optimization
        GameObject[] prefabsArray = authoring.gameObjectPrefabs;
        Array.Sort(prefabsArray, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

        BlobAssetReference<BlobArray<EntityReference>> blobAssetReference;
        //BlobBuilder resources
        using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp))
        {
            //Build new blob root
            ref BlobArray<EntityReference> root = ref blobBuilder.ConstructRoot<BlobArray<EntityReference>>();

            //Allocate memory for the entity array in the root
            BlobBuilderArray<EntityReference> entityIds =
                blobBuilder.Allocate<EntityReference>(ref root, prefabsArray.Length);

            //For all Entity ScriptableObjects found in the list reader
            for (int entityIndex = 0; entityIndex < entityIds.Length; entityIndex++)
            {
                GameObject entityPrefab = prefabsArray[entityIndex];
                Debug.Log($"BAKER: Is part of PrefabAsset: {UnityEditor.PrefabUtility.IsPartOfPrefabAsset(entityPrefab)}");

                DependsOn(entityPrefab);
                RegisterPrefabForBaking(entityPrefab);
                Entity prefabEntity = GetEntity(entityPrefab, TransformUsageFlags.Dynamic);

                //Bake singular data inside blob entry
                EntityReference er = new EntityReference
                {
                    entityKey = new EntityReferenceKey
                    {
                        name = entityPrefab.name,
                    },
                    prefabEntity = prefabEntity
                };
                entityIds[entityIndex] = er;
            }

            //Build BlobAssetReference
            blobAssetReference = blobBuilder.CreateBlobAssetReference<BlobArray<EntityReference>>(Allocator.Persistent);
        }

        AddComponent(entity, new EntityReferencesRegistry
        {
            entityReferenceBlobArrayReference = blobAssetReference
        });

        AddComponent(entity, new EntitiesReferences
        {
            bulletPrefabEntity = GetEntity(authoring.bulletPrefabGameObject, TransformUsageFlags.Dynamic),
            enemyPrefabEntity = GetEntity(authoring.enemyPrefabGameObject, TransformUsageFlags.Dynamic),
            shootLightPrefabEntity = GetEntity(authoring.shootLightPrefabGameObject, TransformUsageFlags.Dynamic),
            scoutPrefabEntity = GetEntity(authoring.scoutPrefabGameObject, TransformUsageFlags.Dynamic),
            soldierPrefabEntity = GetEntity(authoring.soldierPrefabGameObject, TransformUsageFlags.Dynamic)
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
{
    public EntityReferenceKey entityKey;
    public Entity prefabEntity;
}

/// <summary>
/// Contains all <see cref="EntityReference"/>s baked from each prefab in the serialized list.
/// </summary>
public struct EntityReferencesRegistry : IComponentData
{
    /// <summary>
    /// Reference to the BlobArray containing all EntityData.
    /// </summary>
    public BlobAssetReference<BlobArray<EntityReference>> entityReferenceBlobArrayReference;
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
    public abstract FixedString64Bytes GetKey();
}
