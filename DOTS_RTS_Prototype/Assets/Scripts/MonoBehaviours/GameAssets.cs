using UnityEngine;

public class GameAssets : MonoBehaviour
{
    [Header("Physics layers")]
    public const int UNITS_LAYER = 6;
    public const int BUILDINGS_LAYER = 7;

    [Header("Registries")]
    public UnitDataRegistrySO unitRegistrySO;
    public BuildingDataRegistrySO buildingDataRegistrySO;

    [Header("Materials")]
    public Material validGhostMaterial;
    public Material invalidGhostMaterial;


    [Header("Faction IDs")]
    public const int NONE_FACTION = 0;

    public const int PLAYER_FACTION = 1;
    public const int ENEMY_FACTION = 8;

    public static GameAssets Instance { get; private set; }

    /// <summary>
    /// Awake() : MonoBehaviour
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
