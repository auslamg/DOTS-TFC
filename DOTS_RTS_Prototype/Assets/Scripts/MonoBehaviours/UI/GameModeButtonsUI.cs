using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages game mode switching UI between control mode, view mode, and build mode, as well as pause menu access.
/// </summary>
/// <remarks>
/// This component provides UI buttons to switch between gameplay modes (action, selection, view, build)
/// and to open the pause menu. It synchronizes button interactability with the current game mode state
/// managed by <see cref="GameModeManager"/>.
/// </remarks>
public class GameModeButtonsUI : MonoBehaviour
{
    /// <summary>
    /// Button used to switch into action mode (active unit actions).
    /// </summary>
    [SerializeField]
    [Tooltip("Button to enable action mode with unit actions.")]
    Button actionModeButton;

    /// <summary>
    /// Button used to switch into selection mode (unit selection and commands).
    /// </summary>
    [SerializeField]
    [Tooltip("Button to enable selection mode with unit selection and commands.")]
    Button controlModeButton;

    /// <summary>
    /// Button used to switch into view mode (camera-only control).
    /// </summary>
    [SerializeField]
    [Tooltip("Button to enable view mode with camera control.")]
    Button viewModeButton;

    /// <summary>
    /// Button used to open the pause menu and pause the game simulation.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to open the pause menu.")]
    Button pauseMenuButton;

    /// <summary>
    /// UI panel representing the pause menu screen.
    /// </summary>
    [SerializeField]
    [Tooltip("Pause menu screen panel reference, shown when pause is activated.")]
    RectTransform pauseMenuScreen;

    /// <summary>
    /// Global singleton instance for accessing the UI controller.
    /// </summary>
    public static GameModeButtonsUI Instance { get; private set; }

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

    /// <summary>
    /// Unity Awake callback. Registers button listeners and initializes singleton instance.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();

        actionModeButton.onClick.AddListener(() =>
        {
            GameModeManager.Instance.SetGameMode(GameMode.ActionMode);
        });

        controlModeButton.onClick.AddListener(() =>
        {
            GameModeManager.Instance.SetGameMode(GameMode.SelectionMode);
        });

        viewModeButton.onClick.AddListener(() =>
        {
            GameModeManager.Instance.SetGameMode(GameMode.ViewMode);
        });

        pauseMenuButton.onClick.AddListener(() =>
        {
            Time.timeScale = 0;
            pauseMenuScreen.gameObject.SetActive(true);
        });
    }

    /// <summary>
    /// Updates UI interactability based on the currently active game mode.
    /// </summary>
    /// <param name="gameMode">Current active game mode.</param>
    public void UpdateGameModeUI(GameMode gameMode)
    {
        switch (gameMode)
        {
            case GameMode.ActionMode:
                actionModeButton.interactable = false;
                controlModeButton.interactable = true;
                viewModeButton.interactable = true;
                return;

            case GameMode.SelectionMode:
                actionModeButton.interactable = true;
                controlModeButton.interactable = false;
                viewModeButton.interactable = true;
                return;

            case GameMode.ViewMode:
                actionModeButton.interactable = true;
                viewModeButton.interactable = false;
                controlModeButton.interactable = true;
                return;

            case GameMode.BuildMode:
                actionModeButton.interactable = true;
                viewModeButton.interactable = true;
                controlModeButton.interactable = true;
                return;

            default:
                Debug.LogError("Unexisting gameMode triggered.");
                actionModeButton.interactable = true;
                viewModeButton.interactable = true;
                controlModeButton.interactable = true;
                return;
        }
    }
}