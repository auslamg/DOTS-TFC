using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VictoryScreenUI : MonoBehaviour
{
    /// <summary>
    /// Button to go back to the main menu.
    /// </summary>
    [SerializeField]
    [Tooltip("Button to go back to the main menu.")]
    Button mainMenuButton;

    void Awake()
    {
        mainMenuButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1;
            SceneManager.LoadScene(0);
        });
    }

    /// <summary>
    /// Subscribes to game-over events and hides the panel until needed.
    /// </summary>
    void Start()
    {
        WinConditionManager.Instance.OnVictory += WinConditionManager_OnVictory;
        SetVisible(false);
    }

    /// <summary>
    /// Shows game-over UI, pauses time, and applies event message text when provided.
    /// </summary>
    /// <param name="sender">Unused event sender.</param>
    /// <param name="e">Event args containing optional game-over message text.</param>
    private void WinConditionManager_OnVictory(object sender, EventArgs e)
    {
        SetVisible(true);
    }

    /// <summary>
    /// Toggles visibility of the game-over panel.
    /// </summary>
    /// <param name="value"><see langword="true"/> to show the panel; otherwise <see langword="false"/>.</param>
    private void SetVisible(bool value)
    {
        gameObject.SetActive(value);
        /* Time.timeScale = 0; */
    }
}
