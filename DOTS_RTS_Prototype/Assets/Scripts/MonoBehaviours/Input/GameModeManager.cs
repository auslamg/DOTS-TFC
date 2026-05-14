using System;
using UnityEngine;

/// <summary>
/// Manages the current game mode, enabling or disabling input systems and updating UI
/// to reflect the active interaction state.
/// </summary>
public class GameModeManager : MonoBehaviour
{
    /// <summary>
    /// Global singleton instance for accessing the active GameModeManager.
    /// </summary>
    public static GameModeManager Instance { get; private set; }

    /// <summary>
    /// Currently active game mode.
    /// </summary>
    private GameMode activeGameMode;

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
    /// Unity lifecycle method. Initializes singleton instance.
    /// </summary>
    private void Awake()
    {
        InitializeSingleton();
    }

    /// <summary>
    /// Unity lifecycle method. Sets initial game mode and subscribes to selection events.
    /// </summary>
    private void Start()
    {
        SetGameMode(GameMode.ViewMode);
        SelectionManager.Instance.OnSelectionChange += SelectionManager_OnSelectionChange;
    }

    /// <summary>
    /// Callback triggered when selection state changes.
    /// Automatically transitions from SelectionMode to ActionMode if applicable.
    /// </summary>
    private void SelectionManager_OnSelectionChange(object sender, EventArgs e)
    {
        if (activeGameMode == GameMode.SelectionMode)
        {
            SetGameMode(GameMode.ActionMode);
        }
    }

    /// <summary>
    /// Sets the active game mode and enables/disables associated systems accordingly.
    /// Also updates the UI to reflect the current mode.
    /// </summary>
    /// <param name="gameMode">Target game mode to activate.</param>
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
                ActionManager.Instance.gameObject.SetActive(false);
                SelectionManager.Instance.gameObject.SetActive(false);
                TouchCameraController.Instance.gameObject.SetActive(false);
                break;
        }

        GameModeButtonsUI.Instance.UpdateGameModeUI(gameMode);
    }
}

/// <summary>
/// Defines available gameplay interaction modes.
/// </summary>
public enum GameMode
{
    /// <summary>
    /// Mode for direct unit actions and commands.
    /// </summary>
    ActionMode,

    /// <summary>
    /// Mode for selecting and issuing commands to units.
    /// </summary>
    SelectionMode,

    /// <summary>
    /// Mode focused on camera movement and world observation.
    /// </summary>
    ViewMode,

    /// <summary>
    /// Mode for placing buildings in the world.
    /// </summary>
    BuildMode,
}