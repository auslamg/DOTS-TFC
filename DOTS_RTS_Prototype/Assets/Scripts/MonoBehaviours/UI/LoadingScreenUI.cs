using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;
using UnityEngine;

public class LoadingScreenUI : MonoBehaviour
{
    [SerializeField]
    public GameObject loadingScreen;
    public EntitySceneReference subScene;
    private WorldUnmanaged world;
    private Entity sceneEntity;

    void Start()
    {
        var worldManaged = World.DefaultGameObjectInjectionWorld;
        world = worldManaged.Unmanaged;

        loadingScreen.SetActive(true);

        sceneEntity = SceneSystem.LoadSceneAsync(world, subScene);
    }

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