using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the victory screen UI shown when the player wins the game.
/// </summary>
/// <remarks>
/// Subscribes to win-condition events, toggles visibility of the victory panel,
/// and provides navigation back to the main menu.
/// </remarks>
public class VictoryScreenUI : MonoBehaviour
{
    /// <summary>
    /// Button to return to the main menu scene.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to go back to the main menu.")]
    Button mainMenuButton;

    /// <summary>
    /// Registers UI button callbacks.
    /// </summary>
    void Awake()
    {
        mainMenuButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(0);
        });
    }

    /// <summary>
    /// Subscribes to victory events and initializes the UI as hidden.
    /// </summary>
    void Start()
    {
        WinConditionManager.Instance.OnVictory += WinConditionManager_OnVictory;
        SetVisible(false);
    }

    /// <summary>
    /// Handles victory event by displaying the victory screen.
    /// </summary>
    /// <param name="sender">Unused event sender.</param>
    /// <param name="e">Event arguments (not used).</param>
    private void WinConditionManager_OnVictory(object sender, EventArgs e)
    {
        SetVisible(true);
    }

    /// <summary>
    /// Toggles the visibility of the victory panel.
    /// </summary>
    /// <param name="value"><see langword="true"/> to show the panel; otherwise <see langword="false"/>.</param>
    private void SetVisible(bool value)
    {
        gameObject.SetActive(value);
        /* Time.timeScale = 0; */
    }
}