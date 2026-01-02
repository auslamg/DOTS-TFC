using Unity.Entities;
using UnityEngine;

class EntitiesReferencesAuthoring : MonoBehaviour
{
    public GameObject bulletPrefabGameObject;
    public GameObject enemyPrefabGameObject;


}

class EntitiesReferencesBaker : Baker<EntitiesReferencesAuthoring>
{
    public override void Bake(EntitiesReferencesAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EntitiesReferences
        {
            bulletPrefabEntity = GetEntity(authoring.bulletPrefabGameObject, TransformUsageFlags.Dynamic),
            enemyPrefabEntity = GetEntity(authoring.enemyPrefabGameObject, TransformUsageFlags.Dynamic)
        });
        
    }
}

struct EntitiesReferences : IComponentData
{
    public Entity bulletPrefabEntity;
    public Entity enemyPrefabEntity;
}

