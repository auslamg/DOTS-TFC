using System;
using TMPro;
using UnityEngine;

/// <summary>
/// UI controller for displaying Horde mode wave information, countdowns, and remaining enemy status.
/// </summary>
/// <remarks>
/// This component listens to <see cref="WinConditionManager"/> and <see cref="HordeManager"/> events/state
/// to update the wave timer text, subtext messages, and remaining kill requirements during gameplay.
/// </remarks>
public class HordeWaveUI : MonoBehaviour
{
    /// <summary>
    /// UI text displaying the current wave state or countdown timer.
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI timerText;

    /// <summary>
    /// Secondary UI text displaying contextual wave information or objectives.
    /// </summary>
    [SerializeField]
    private TextMeshProUGUI subText;

    /// <summary>
    /// Cached number of remaining kills required to satisfy the win condition.
    /// </summary>
    [SerializeField]
    private int remainingKills;

    /// <summary>
    /// Unity Start callback. Subscribes to enemy count updates from <see cref="WinConditionManager"/>.
    /// </summary>
    void Start()
    {
        WinConditionManager.Instance.OnRemainingEnemiesChange += Hapapa;
    }

    /// <summary>
    /// Event handler invoked when remaining enemy count changes.
    /// Updates internal kill requirement tracking.
    /// </summary>
    /// <param name="sender">Event source.</param>
    /// <param name="e">Event data containing enemy counts.</param>
    private void Hapapa(object sender, RemainingEnemiesEventArgs e)
    {
        remainingKills = e.remainingEnemies - e.maxEnemiesToWin;
    }

    /// <summary>
    /// Unity Update loop. Refreshes UI based on Horde wave state and countdown timers.
    /// </summary>
    private void Update()
    {
        if (HordeManager.Instance == null)
            return;

        if (!HordeManager.Instance.isCountingDownToNextWave)
        {
            timerText.text = !HordeManager.Instance.finalWave ?
                $"WAVE {HordeManager.Instance.currentWaveIndex + 1}" :
                $"FINAL WAVE";

            subText.text = !HordeManager.Instance.finalWave ?
                $"Survive the horde!" :
                $"Eliminate all remaining orcs: {remainingKills}";

            return;
        }
        else
        {
            float time = HordeManager.Instance.remainingNextWaveTime;

            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);

            timerText.text = $"NEXT WAVE IN {minutes:00}:{seconds:00}";
            subText.text = $"Prepare for the next wave...";

            timerText.color = time <= 5f
                ? Color.red
                : Color.white;

            subText.color = time <= 5f
                ? new Color(.95f, .3f, .175f, 1)
                : new Color(.8f, .8f, .8f, 1);
        }
    }
}