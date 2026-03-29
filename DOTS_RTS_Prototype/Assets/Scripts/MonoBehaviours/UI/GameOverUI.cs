using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays game-over state and message when the DOTS game-over event is raised.
/// </summary>
/// <remarks>
/// The panel starts hidden, subscribes to <see cref="DOTSEventManager.OnGameOver"/>,
/// and pauses gameplay by setting <see cref="Time.timeScale"/> to zero once game over occurs.
/// </remarks>
public class GameOverUI : MonoBehaviour
{
    /// <summary>
    /// Text element used to show the game-over message.
    /// </summary>
    [SerializeField]
    [Tooltip("Text element that displays the game-over message.")]
    TMP_Text messageText;

    /// <summary>
    /// Subscribes to game-over events and hides the panel until needed.
    /// </summary>
    void Start()
    {
        DOTSEventManager.Instance.OnGameOver += DOTSEventManager_OnGameOver;
        SetVisible(false);
    }

    /// <summary>
    /// Shows game-over UI, pauses time, and applies event message text when provided.
    /// </summary>
    /// <param name="sender">Unused event sender.</param>
    /// <param name="e">Event args containing optional game-over message text.</param>
    private void DOTSEventManager_OnGameOver(object sender, MsgEventArgs e)
    {
        SetVisible(true);
        Time.timeScale = 0f;

        if (e.msg != null)
        {
            messageText.text = e.msg;
        }
    }

    /// <summary>
    /// Toggles visibility of the game-over panel.
    /// </summary>
    /// <param name="value"><see langword="true"/> to show the panel; otherwise <see langword="false"/>.</param>
    private void SetVisible(bool value)
    {
        gameObject.SetActive(value);
    }
}
