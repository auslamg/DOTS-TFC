using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

class OccluderAuthoring : MonoBehaviour
{
    public BoxCollider occlusionBox;
    public GridParametersSO gridParameters;
}

class OccluderBaker : Baker<OccluderAuthoring>
{
    public override void Bake(OccluderAuthoring authoring)
    {
        var box = authoring.occlusionBox;
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
        AddComponent(entity, new Occluder
        {
            occlusionFootprint = new int2(x, y),
            isAccountedFor = false
        });
    }
}

public struct Occluder : IComponentData
{
    public int2 occlusionFootprint;
    public bool isAccountedFor;
}