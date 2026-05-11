using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    /// <summary>
    /// Global singleton access to unit selection behavior.
    /// </summary>
    public static GameModeManager Instance { get; private set; }

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
    void Awake()
    {
        InitializeSingleton();
    }

    void Start()
    {
        SetGameMode(GameMode.ViewMode);
    }

    public void SetGameMode(GameMode gameMode)
    {
        switch (gameMode)
        {
            case GameMode.ActionMode:
                Debug.LogError("NOT IMPLEMENTED.");
                break;
            case GameMode.ControlMode:
                UnitSelectionManager.Instance.gameObject.SetActive(true);
                TouchCameraController.Instance.gameObject.SetActive(false);
                break;
            case GameMode.ViewMode:
                UnitSelectionManager.Instance.gameObject.SetActive(false);
                TouchCameraController.Instance.gameObject.SetActive(true);
                break;
            default:
                Debug.LogError("Unexisting gameMode triggered.");
                UnitSelectionManager.Instance.gameObject.SetActive(false);
                UnitSelectionManager.Instance.gameObject.SetActive(false);
                TouchCameraController.Instance.gameObject.SetActive(false);
                break;
        }

        GameModeButtonsUI.Instance.UpdateGameModeUI(gameMode);
    }
}

public enum GameMode
{
    ActionMode,
    ControlMode,
    ViewMode,
}
