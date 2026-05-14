using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Handles cross-scene load requests and ensures a single persistent entry point for triggering game load operations.
/// </summary>
/// <remarks>
/// This component survives scene transitions and delegates actual loading work to <see cref="LoadManager"/>.
/// It is intended as a lightweight bridge to safely request a load operation from UI or initialization scenes.
/// </remarks>
public class LoadRequest : MonoBehaviour
{
    /// <summary>
    /// Global singleton instance of the load request handler.
    /// </summary>
    public static LoadRequest Instance { get; private set; }

    /// <summary>
    /// Indicates whether a load operation has already been triggered.
    /// </summary>
    public bool loaded = false;

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
    /// Unity Awake callback. Initializes singleton and persists this object across scenes.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Requests a game load operation from <see cref="LoadManager"/> if available and not already triggered.
    /// </summary>
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