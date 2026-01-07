using Unity.Entities;
using UnityEngine;

//REVIEW: healthbar can be refactored into being a base unit component or a bar component instead of extensive reference, sacrificing project clarity for performance.
class HealthBarAuthoring : MonoBehaviour
{
    public GameObject visualBarGameObject;
    public GameObject healthGameObject;
}

class HealthBarBaker : Baker<HealthBarAuthoring>
{
    public override void Bake(HealthBarAuthoring authoring)
    {
        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new HealthBar
        {
            visualBarEntity = GetEntity(authoring.visualBarGameObject, TransformUsageFlags.NonUniformScale),
            healthEntity = GetEntity(authoring.healthGameObject, TransformUsageFlags.Dynamic)
        });
    }
}

public struct HealthBar : IComponentData
{
    public Entity visualBarEntity;
    public Entity healthEntity;
}
