using UnityEngine;

/// <summary>
/// Global asset and configuration singleton used across gameplay and UI systems.
/// Provides shared registries, materials, and constant gameplay configuration values.
/// </summary>
/// <remarks>
/// This class acts as a central reference point for commonly used ScriptableObject
/// registries, rendering materials, physics layer indices, and faction identifiers.
/// </remarks>
public class GameAssets : MonoBehaviour
{
    [Header("Physics layers")]

    /// <summary>
    /// Physics layer index used for unit entities.
    /// </summary>
    public const int UNITS_LAYER = 6;

    /// <summary>
    /// Physics layer index used for building entities.
    /// </summary>
    public const int BUILDINGS_LAYER = 7;

    /// <summary>
    /// Physics layer index used for resource source entities.
    /// </summary>
    public const int RESOURCE_SOURCES_LAYER = 8;

    /// <summary>
    /// Physics layer index used for pathfinding obstruction markers.
    /// </summary>
    public const int OBSTRUCTION_LAYER = 9;

    [Header("Registries")]

    /// <summary>
    /// Registry containing all unit definitions used by gameplay systems and UI.
    /// </summary>
    [SerializeField]
    [Tooltip("Unit definition registry used by gameplay and UI systems.")]
    public UnitDataRegistrySO unitRegistrySO;

    /// <summary>
    /// Registry containing all building definitions used by gameplay systems and UI.
    /// </summary>
    [SerializeField]
    [Tooltip("Building definition registry used by gameplay and UI systems.")]
    public BuildingDataRegistrySO buildingDataRegistrySO;

    [Header("Materials")]

    /// <summary>
    /// Material used for valid building placement ghost previews.
    /// </summary>
    [SerializeField]
    [Tooltip("Material used by building ghost previews when placement is valid.")]
    public Material validGhostMaterial;

    /// <summary>
    /// Material used for invalid building placement ghost previews.
    /// </summary>
    [SerializeField]
    [Tooltip("Material used by building ghost previews when placement is invalid.")]
    public Material invalidGhostMaterial;

    [Header("Faction IDs")]

    /// <summary>
    /// Faction identifier representing no faction ownership.
    /// </summary>
    public const int NONE_FACTION = 0;

    /// <summary>
    /// Faction identifier representing player-controlled entities.
    /// </summary>
    public const int PLAYER_FACTION = 1;

    /// <summary>
    /// Faction identifier representing enemy-controlled entities.
    /// </summary>
    public const int ENEMY_FACTION = 8;

    /// <summary>
    /// Global singleton instance for accessing shared game assets.
    /// </summary>
    public static GameAssets Instance { get; private set; }

    /// <summary>
    /// Ensures singleton instance validity.
    /// </summary>
    private void InitializeSingleton()
    {
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

    /// <summary>
    /// Unity Awake callback. Initializes singleton instance.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();
    }
}