using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Managed component for the <see cref="Occluder"/> unmanaged component.
/// </summary>
class OccluderAuthoring : MonoBehaviour
{
    /// <summary>
    /// Box collider defining the occlusion area.
    /// </summary>
    [Tooltip("Box collider defining the occlusion area.")]

    public BoxCollider occlusionBox;

    /// <summary>
    /// Reference to the grid parameters scriptable object.
    /// </summary>
    [Tooltip("Reference to the grid parameters scriptable object.")]
    public GridParametersSO gridParameters;
}

/// <summary>
/// Baker for the <see cref="Occluder"/> unmanaged component.
/// </summary>
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
            isAccountedFor = false,
            markedForDeletion = false
        });
    }
}

/// <summary>
/// Component data for occlusion areas in the world grid.
/// </summary>
public struct Occluder : IComponentData
{
    /// <summary>
    /// The size of the occlusion footprint in grid cells (X, Y).
    /// </summary>
    public int2 occlusionFootprint;

    /// <summary>
    /// Whether this occluder has been accounted for in the grid.
    /// </summary>
    public bool isAccountedFor;

    /// <summary>
    /// Whether this occluder is marked for deletion.
    /// </summary>
    public bool markedForDeletion;
}