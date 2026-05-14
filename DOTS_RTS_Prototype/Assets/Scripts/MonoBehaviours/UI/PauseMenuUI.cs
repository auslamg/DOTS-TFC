using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the pause menu UI with options to resume, save, load, or return to main menu.
/// </summary>
/// <remarks>
/// This component provides pause menu functionality including game time management and scene navigation.
/// Resume resumes gameplay, save and load persist or load the game from/to a file, and main menu navigation
/// returns to the title screen.
/// </remarks>
public class PauseMenuUI : MonoBehaviour
{
    /// <summary>
    /// Button to resume the game.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to resume the game.")]
    Button resumeButton;

    /// <summary>
    /// Button to save the current game.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to save the current game.")]
    Button saveButton;

    /// <summary>
    /// Button to load a saved game.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to load a saved game.")]
    Button loadButton;

    /// <summary>
    /// Button to go back to the main menu.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to go back to the main menu.")]
    Button mainMenuButton;

    /// <summary>
    /// Reference to the game mode UI container that is hidden while the game is paused.
    /// </summary>
    [SerializeField]
    [Tooltip("Game mode ui reference for disabling when paused.")]
    RectTransform gameModeButtonsUI;

    /// <summary>
    /// Name of the save file used to determine whether a valid save exists.
    /// </summary>
    [SerializeField]
    [Tooltip("Save file name for path checking.")]
    string saveFileName;

    /// <summary>
    /// Wires button listeners to handle pause menu interactions and initializes default state.
    /// </summary>
    void Awake()
    {
        resumeButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1;
            gameModeButtonsUI.gameObject.SetActive(true);
            gameObject.SetActive(false);
        });

        saveButton.onClick.AddListener(() =>
        {
            SaveManager.Instance.SaveGame();
        });

        loadButton.onClick.AddListener(() =>
        {
            LoadManager.Instance.LoadGame();

            Time.timeScale = 1;
            gameModeButtonsUI.gameObject.SetActive(true);
            gameObject.SetActive(false);
        });

        mainMenuButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(0);
        });

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Updates the availability of the load button based on whether a save file exists.
    /// </summary>
    void Update()
    {
        if (!LoadManager.BinarySaveFileExists(saveFileName) &&
            !LoadManager.JsonSaveFileExists(saveFileName))
        {
            loadButton.interactable = false;
        }
    }
}