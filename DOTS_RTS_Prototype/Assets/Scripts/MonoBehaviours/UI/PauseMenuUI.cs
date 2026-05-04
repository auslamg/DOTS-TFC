using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the pause menu UI with options to resume, save, load, or return to main menu.
/// </summary>
/// <remarks>
/// This component provides pause menu functionality including game time management and scene navigation.
/// Resume resumes gameplay, save and load have placeholder implementations (TODO), and main menu navigation
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
    /// Game mode ui reference for disabling when paused.
    /// </summary>
    [SerializeField]
    [Tooltip("Game mode ui reference for disabling when paused.")]
    RectTransform gameModeButtonsUI;

    /// <summary>
    /// Wires button listeners to handle pause menu interactions.
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
            // TODO: Implement
            /* LoadGame */
        });
        mainMenuButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(0);
        });

        gameObject.SetActive(false);
    }
}
