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
    /// Button to enable control mode with unit selection and disable camera view mode.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to enable control mode with unit selection and disable camera view mode.")]
    Button controlModeButton;

    /// <summary>
    /// Button to enable view mode with camera control and disable unit selection.
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
    /// Wires button listeners to handle game mode switching and pause menu access.
    /// </summary>
    void Awake()
    {
        controlModeButton.onClick.AddListener(() =>
        {
            UnitSelectionManager.Instance.gameObject.SetActive(true);
            TouchCameraController.Instance.gameObject.SetActive(false);
            controlModeButton.interactable = false;
            viewModeButton.interactable = true;

        });
        viewModeButton.onClick.AddListener(() =>
        {
            UnitSelectionManager.Instance.gameObject.SetActive(false);
            TouchCameraController.Instance.gameObject.SetActive(true);
            viewModeButton.interactable = false;
            controlModeButton.interactable = true;
        });
        pauseMenuButton.onClick.AddListener(() =>
        {
            Time.timeScale = 0;
            pauseMenuScreen.gameObject.SetActive(true);
        });
    }
}
