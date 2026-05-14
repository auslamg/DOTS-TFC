using System;
using UnityEngine;

/// <summary>
/// Manages the current game mode, controlling which input systems are active
/// and updating the UI to reflect the current mode.
/// </summary>
public class GameModeManager : MonoBehaviour
{
    /// <summary>
    /// Global singleton access to the game mode manager.
    /// </summary>
    public static GameModeManager Instance { get; private set; }

    private GameMode activeGameMode;

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
    /// Initializes the singleton instance.
    /// </summary>
    void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// Sets the initial game mode to ViewMode.
    /// </summary>
    void Start()
    {
        SetGameMode(GameMode.ViewMode);
        SelectionManager.Instance.OnSelectionChange += SelectionManager_OnSelectionChange;
    }

    private void SelectionManager_OnSelectionChange(object sender, EventArgs e)
    {
        if (activeGameMode == GameMode.SelectionMode)
        {
            SetGameMode(GameMode.ActionMode);
        }
    }

    /// <summary>
    /// Sets the current game mode, activating/deactivating relevant systems and updating the UI.
    /// </summary>
    /// <param name="gameMode">The game mode to switch to.</param>
    public void SetGameMode(GameMode gameMode)
    {
        switch (gameMode)
        {
            case GameMode.ActionMode:
                ActionManager.Instance.gameObject.SetActive(true);
                SelectionManager.Instance.gameObject.SetActive(false);
                TouchCameraController.Instance.gameObject.SetActive(false);
                activeGameMode = GameMode.ActionMode;
                break;
            case GameMode.SelectionMode:
                ActionManager.Instance.gameObject.SetActive(false);
                SelectionManager.Instance.gameObject.SetActive(true);
                TouchCameraController.Instance.gameObject.SetActive(false);
                activeGameMode = GameMode.SelectionMode;
                break;
            case GameMode.ViewMode:
                ActionManager.Instance.gameObject.SetActive(false);
                SelectionManager.Instance.gameObject.SetActive(false);
                TouchCameraController.Instance.gameObject.SetActive(true);
                activeGameMode = GameMode.ViewMode;
                break;
            case GameMode.BuildMode:
                ActionManager.Instance.gameObject.SetActive(false);
                SelectionManager.Instance.gameObject.SetActive(false);
                TouchCameraController.Instance.gameObject.SetActive(false);
                activeGameMode = GameMode.BuildMode;
                break;
            default:
                Debug.LogError("Unexisting gameMode triggered.");
                SelectionManager.Instance.gameObject.SetActive(false);
                SelectionManager.Instance.gameObject.SetActive(false);
                TouchCameraController.Instance.gameObject.SetActive(false);
                break;
        }

        GameModeButtonsUI.Instance.UpdateGameModeUI(gameMode);
    }
}

/// <summary>
/// Enumeration of available game modes.
/// </summary>
public enum GameMode
{
    /// <summary>
    /// Action mode for performing specific actions (not yet implemented).
    /// </summary>
    ActionMode,

    /// <summary>
    /// Control mode for unit selection and commanding.
    /// </summary>
    SelectionMode,

    /// <summary>
    /// View mode for camera movement and observation.
    /// </summary>
    ViewMode,

    /// <summary>
    /// Building mode for building placement without inconveniencies. Unavailable from game mode buttons.
    /// </summary>
    BuildMode,
}
