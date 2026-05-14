using Unity.VisualScripting;
using UnityEngine;

public class LoadRequest : MonoBehaviour
{
    /// <summary>
    /// Global singleton access to the DOTS event bridge.
    /// </summary>
    public static LoadRequest Instance { get; private set; }

    /// <summary>
    /// Initializes singleton instance state.
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

    public bool loaded = false;

    private void Awake()
    {
        InitializeSingleton();
        DontDestroyOnLoad(gameObject);
    }

    public void LoadGame()
    {
        if (!loaded)
        {
            if (LoadManager.Instance != null)
            {
                LoadManager.Instance.LoadGame();
                Destroy(gameObject);
            }
        }
    }
}
