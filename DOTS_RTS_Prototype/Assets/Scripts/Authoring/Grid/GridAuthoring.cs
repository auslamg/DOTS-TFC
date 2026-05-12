using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System.Linq;

/// <summary>
/// Managed component for the <see cref="GridDataParameters"/> unmanaged component.
/// </summary>
/// <remarks>
/// These parameters are used just once to build the grid rather than to act as a component of an entity.
/// The entity containing this data will be removed after grid construction.
/// </remarks>
class GridAuthoring : MonoBehaviour
{
    /// <summary>
    /// Reference to the grid parameters scriptable object.
    /// </summary>
    [Tooltip("Reference to the grid parameters scriptable object.")]
    public GridParametersSO gridParameters;

    /// <summary>
    /// Scene singleton instance for managed-side access.
    /// </summary>
    public static GridAuthoring Instance { get; private set; }

    /// <summary>
    /// Initializes singleton instance state.
    /// </summary>
    void Awake()
    {
        // Initialize singleton instance state.
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
/// Baker for the <see cref="GridDataParameters"/> unmanaged component.
/// </summary>
class GridBaker : Baker<GridAuthoring>
{
    public override void Bake(GridAuthoring authoring)
    {
        var gridParameters = authoring.gridParameters;
        Entity entity = GetEntity(TransformUsageFlags.WorldSpace);
        AddComponent(entity, new GridDataParameters
        {
            width = gridParameters.size,
            height = gridParameters.size,
            gridCellSize = gridParameters.gridCellSize,
            isInitialized = false
        });
    }
}

/// <summary>
/// Singleton component containing baked grid configuration.
/// </summary>
/// <remarks>
/// The grid settings are authored in the scene and baked into this singleton component for runtime systems.
/// Access this component through <see cref="SystemAPI.GetSingleton()"/>.
/// </remarks>
public struct GridDataParameters : IComponentData
{
    /// <summary>
    /// Grid width in cells.
    /// </summary>
    public int width;
    /// <summary>
    /// Grid height in cells.
    /// </summary>
    public int height;
    /// <summary>
    /// Size of each cell side in world units.
    /// </summary>
    public float gridCellSize;
    /// <summary>
    /// Grid width in cells.
    /// </summary>
    public bool isInitialized;
}
