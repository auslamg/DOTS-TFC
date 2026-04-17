using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using System.Linq;

/// <summary>
/// Authoring component that bakes data into <see cref="GridDataParameters"/>.
/// </summary>
/// <remarks>
/// Behaves as a scene singleton.
/// </remarks>
class GridAuthoring : MonoBehaviour
{
    /// <summary>
    /// Grid width in cells.
    /// </summary>
    [SerializeField]
    [Tooltip("Grid width in cells.")]
    public int width = 20;
    /// <summary>
    /// Grid height in cells.
    /// </summary>
    [SerializeField]
    [Tooltip("Grid height in cells.")]
    public int height = 20;
    /// <summary>
    /// Size of a single grid cell side in world units.
    /// </summary>
    [SerializeField]
    [Tooltip("Size of a single grid cell side in world units.")]
    public float gridCellSize = 5;

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
        Entity entity = GetEntity(TransformUsageFlags.WorldSpace);
        AddComponent(entity, new GridDataParameters
        {
            width = authoring.width,
            height = authoring.height,
            gridCellSize = authoring.gridCellSize,
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
