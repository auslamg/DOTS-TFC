
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the main menu UI interactions, providing buttons to start the game or exit the application.
/// </summary>
/// <remarks>
/// This component handles scene navigation and application lifecycle events.
/// The play button loads the game scene, and the quit button closes the application.
/// </remarks>
public class MainMenuUI : MonoBehaviour
{
    /// <summary>
    /// Button to start the game and load the main game scene.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to start the game and load the main game scene.")]
    Button newGameButton;

    /// <summary>
    /// Button to load a saved game.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to load a saved game.")]
    Button loadButton;

    /// <summary>
    /// Button to quit the application.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to quit the application.")]
    Button quitButton;

    /// <summary>
    /// Prefab of Load Request game object.
    /// </summary>
    [SerializeField]
    [Tooltip("Prefab of Load Request game object.")]
    GameObject loadRequestPrefab;

    /// <summary>
    /// Save file name for path checking.
    /// </summary>
    [SerializeField]
    [Tooltip("Save file name for path checking.")]
    string saveFileName;

    /// <summary>
    /// Wires button listeners to handle play and quit actions.
    /// </summary>
    void Awake()
    {
        newGameButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(1);
        });
        loadButton.onClick.AddListener(() =>
        {
            Instantiate(loadRequestPrefab);
            SceneManager.LoadScene(1);
        });
        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });

        if (!LoadManager.BinarySaveFileExists(saveFileName) &&
            !LoadManager.JsonSaveFileExists(saveFileName))
        {
            loadButton.interactable = false;
        }
    }
}
