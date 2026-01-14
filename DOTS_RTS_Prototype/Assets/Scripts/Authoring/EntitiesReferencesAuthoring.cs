using Unity.Entities;
using UnityEngine;

/// <summary>
/// Managed component for the <c>EntitiesReferences</c> unmanaged component.
/// </summary>
/// <remarks>
/// The component is a Singleton.
/// </remarks>
class EntitiesReferencesAuthoring : MonoBehaviour
{
    public GameObject bulletPrefabGameObject;
    public GameObject enemyPrefabGameObject;
    public GameObject shootLightPrefabGameObject;

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
/// Baker for the <c>EntitiesReferences</c> unmanaged component.
/// </summary>
class EntitiesReferencesBaker : Baker<EntitiesReferencesAuthoring>
{
    public override void Bake(EntitiesReferencesAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EntitiesReferences
        {
            bulletPrefabEntity = GetEntity(authoring.bulletPrefabGameObject, TransformUsageFlags.Dynamic),
            enemyPrefabEntity = GetEntity(authoring.enemyPrefabGameObject, TransformUsageFlags.Dynamic),
            shootLightPrefabEntity = GetEntity(authoring.shootLightPrefabGameObject, TransformUsageFlags.Dynamic),
        });
        
    }
}

/// <summary>
/// <c>EntitiesReferences</c> entity component.
/// Used for passing down references to Entity prefabs.
/// Must set values for all fields on instantiation.
/// </summary>
/// <remarks>
/// The component is a Singleton, which should be obtained through <see cref="SystemAPI.GetSingleton()"/>.
/// </remarks>
struct EntitiesReferences : IComponentData
{
    public Entity bulletPrefabEntity;
    public Entity enemyPrefabEntity;
    public Entity shootLightPrefabEntity;
}

