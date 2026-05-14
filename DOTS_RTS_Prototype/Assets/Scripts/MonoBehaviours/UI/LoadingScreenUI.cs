using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;
using UnityEngine;

/// <summary>
/// Handles asynchronous subscene loading and controls the visibility of a loading screen UI.
/// </summary>
/// <remarks>
/// This component triggers loading of a DOTS subscene and waits until it is fully loaded before
/// hiding the loading screen, resuming gameplay time, and optionally triggering a game load request.
/// </remarks>
public class LoadingScreenUI : MonoBehaviour
{
    /// <summary>
    /// UI object displayed while the subscene is loading.
    /// </summary>
    [SerializeField]
    public GameObject loadingScreen;

    /// <summary>
    /// Reference to the ECS subscene that will be loaded asynchronously.
    /// </summary>
    public EntitySceneReference subScene;

    /// <summary>
    /// Unmanaged ECS world reference used for scene loading operations.
    /// </summary>
    private WorldUnmanaged world;

    /// <summary>
    /// Entity handle representing the asynchronously loaded scene.
    /// </summary>
    private Entity sceneEntity;

    /// <summary>
    /// Initializes the loading process, enables the loading screen, and begins subscene loading.
    /// </summary>
    void Start()
    {
        var worldManaged = World.DefaultGameObjectInjectionWorld;
        world = worldManaged.Unmanaged;

        loadingScreen.SetActive(true);

        sceneEntity = SceneSystem.LoadSceneAsync(world, subScene);
    }

    /// <summary>
    /// Monitors subscene loading progress and finalizes initialization once loading completes.
    /// </summary>
    void Update()
    {
        if (SceneSystem.IsSceneLoaded(world, sceneEntity))
        {
            loadingScreen.SetActive(false);
            enabled = false;

            if (LoadRequest.Instance != null)
            {
                LoadRequest.Instance.LoadGame();
            }

            Time.timeScale = 1;
        }
    }
}