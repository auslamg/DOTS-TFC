using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages game mode switching UI between control mode and view mode, with pause menu access.
/// </summary>
/// <remarks>
/// This component provides buttons to switch between unit control mode and camera view mode,
/// as well as access to the pause menu. It manages the activation of related systems and UI panels
/// when switching modes or pausing the game.
/// </remarks>
public class GameModeButtonsUI : MonoBehaviour
{
    /// <summary>
    /// Button to enable action mode with active selection and disable camera view mode and unit selection.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to enable control mode with unit selection and disable camera view mode.")]
    Button actionModeButton;

    /// <summary>
    /// Button to enable control mode with unit selection and disable camera view mode and selection actions.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to enable control mode with unit selection and disable camera view mode.")]
    Button controlModeButton;

    /// <summary>
    /// Button to enable view mode with camera control and disable unit selection and selection actions.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to enable view mode with camera control and disable unit selection.")]
    Button viewModeButton;

    /// <summary>
    /// Button to pause the game and open the pause menu.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to pause the game and open the pause menu.")]
    Button pauseMenuButton;

    /// <summary>
    /// Pause menu screen panel reference, shown when pause is activated.
    /// </summary>
    [SerializeField]
    [Tooltip("Pause menu screen panel reference, shown when pause is activated.")]
    RectTransform pauseMenuScreen;

    /// <summary>
    /// Global singleton access to unit selection behavior.
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
    /// Wires button listeners to handle game mode switching and pause menu access.
    /// </summary>
    void Awake()
    {
        InitializeSingleton();
        actionModeButton.onClick.AddListener(() =>
        {
            GameModeManager.Instance.SetGameMode(GameMode.ActionMode);
        });
        controlModeButton.onClick.AddListener(() =>
        {
            GameModeManager.Instance.SetGameMode(GameMode.ControlMode);
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

    public void UpdateGameModeUI(GameMode gameMode)
    {
        switch (gameMode)
        {
            case GameMode.ActionMode:
                actionModeButton.interactable = false;
                controlModeButton.interactable = true;
                viewModeButton.interactable = true;
                return;
            case GameMode.ControlMode:
                actionModeButton.interactable = true;
                controlModeButton.interactable = false;
                viewModeButton.interactable = true;
                return;
            case GameMode.ViewMode:
                actionModeButton.interactable = true;
                viewModeButton.interactable = false;
                controlModeButton.interactable = true;
                return;
            default:
                Debug.LogError("Unexisting gameMode triggered.");
                SelectionManager.Instance.gameObject.SetActive(false);
                SelectionManager.Instance.gameObject.SetActive(false);
                TouchCameraController.Instance.gameObject.SetActive(false);
                return;
        }
    }
}
