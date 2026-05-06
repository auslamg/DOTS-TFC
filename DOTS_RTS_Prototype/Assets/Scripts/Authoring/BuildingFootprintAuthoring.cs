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

        // Swap axis if the collider is rotated
        {
            float yRotation = authoring.transform.eulerAngles.y;
            int rotationSteps = Mathf.RoundToInt(yRotation / 90f) % 4;
            bool rotatedPerpendicular = rotationSteps == 1 || rotationSteps == 3;

            if (rotatedPerpendicular)
            {
                Debug.Log("[Footprint] Detected rotated collider");
                float temp = x;
                x = y;
                y = (int)temp;
            }
        }

        float3 worldSpaceCenter = authoring.transform.position;
        // bottom-left in XZ plane
        float3 bottomLeft = new float3(
            worldSpaceCenter.x - 2*x,
            worldSpaceCenter.y,
            worldSpaceCenter.z - 2*y
        );

        Entity entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new BuildingFootprint
        {
            occlusionSize = new int2(x, y),
            isAccountedFor = false
        });
    }
}

public struct BuildingFootprint : IComponentData
{
    public int2 occlusionSize;
    public bool isAccountedFor;
}