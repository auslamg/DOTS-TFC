using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class BuildingFootprintAuthoring : MonoBehaviour
{
    public GridParametersSO gridParameters;
}

class BuildingFootprintAuthoringBaker : Baker<BuildingFootprintAuthoring>
{
    public override void Bake(BuildingFootprintAuthoring authoring)
    {
        var box = authoring.GetComponent<BoxCollider>();
        var gridParameters = authoring.gridParameters;

        // Account for transform scale.
        float3 lossyScale = authoring.transform.lossyScale;
        float3 worldSpaceSize = box.size * lossyScale;

        float xSize = worldSpaceSize.x;
        float ySize = worldSpaceSize.z;

        // Max inclusive bounds.
        int x = (int)math.ceil(xSize / gridParameters.gridCellSize);
        int y = (int)math.ceil(ySize / gridParameters.gridCellSize);

        float3 worldSpaceCenter = authoring.transform.TransformPoint(box.center);
        // bottom-left in XZ plane
        float3 bottomLeft = new float3(
            worldSpaceCenter.x - worldSpaceSize.x * 0.5f,
            worldSpaceCenter.y,
            worldSpaceCenter.z - worldSpaceSize.z * 0.5f
        );

        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new BuildingFootprint
        {
            occlusionSize = new int2(x, y),
            origin = (x == 1 && y == 1) ? authoring.transform.position : bottomLeft,
            isAccountedFor = false
        });
    }
}

public struct BuildingFootprint : IComponentData
{
    public int2 occlusionSize;
    public float3 origin;
    public bool isAccountedFor;
}