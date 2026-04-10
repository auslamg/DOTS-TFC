using UnityEngine;

/// <summary>
/// Global asset/configuration singleton used by gameplay and UI MonoBehaviours.
/// </summary>
/// <remarks>
/// Holds shared ScriptableObject registries, placement materials, and common layer/faction constants.
/// </remarks>
public class GameAssets : MonoBehaviour
{
    [Header("Physics layers")]
    /// <summary>
    /// Layer index used by unit entities.
    /// </summary>
    public const int UNITS_LAYER = 6;

    /// <summary>
    /// Layer index used by building entities.
    /// </summary>
    public const int BUILDINGS_LAYER = 7;
    /// <summary>
    /// Layer index used by building entities.
    /// </summary>
    public const int OBSTRUCTION_LAYER = 9;

    [Header("Registries")]
    /// <summary>
    /// Unit definition registry used by gameplay and UI systems.
    /// </summary>
    [SerializeField]
    [Tooltip("Unit definition registry used by gameplay and UI systems.")]
    public UnitDataRegistrySO unitRegistrySO;

    /// <summary>
    /// Building definition registry used by gameplay and UI systems.
    /// </summary>
    [SerializeField]
    [Tooltip("Building definition registry used by gameplay and UI systems.")]
    public BuildingDataRegistrySO buildingDataRegistrySO;

    [Header("Materials")]
    /// <summary>
    /// Material used for valid placement ghost previews.
    /// </summary>
    [SerializeField]
    [Tooltip("Material used by building ghost previews when placement is valid.")]
    public Material validGhostMaterial;

    /// <summary>
    /// Material used for invalid placement ghost previews.
    /// </summary>
    [SerializeField]
    [Tooltip("Material used by building ghost previews when placement is invalid.")]
    public Material invalidGhostMaterial;


    [Header("Faction IDs")]
    /// <summary>
    /// Faction constant used when no faction is assigned.
    /// </summary>
    public const int NONE_FACTION = 0;

    /// <summary>
    /// Faction constant used by player-owned entities.
    /// </summary>
    public const int PLAYER_FACTION = 1;

    /// <summary>
    /// Faction constant used by enemy-owned entities.
    /// </summary>
    public const int ENEMY_FACTION = 8;

    /// <summary>
    /// Global singleton access to shared game assets.
    /// </summary>
    public static GameAssets Instance { get; private set; }

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
