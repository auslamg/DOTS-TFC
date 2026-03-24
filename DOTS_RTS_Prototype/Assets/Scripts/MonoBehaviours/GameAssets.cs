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
    public Material ghostMaterial;

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
