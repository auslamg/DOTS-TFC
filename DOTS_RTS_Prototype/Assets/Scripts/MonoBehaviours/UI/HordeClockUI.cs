using TMPro;
using UnityEngine;

public class HordeClockUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    private void Update()
    {
        if (HordeManager.Instance == null)
            return;

        if (!HordeManager.Instance.IsCountingDownToNextWave)
        {
            timerText.text = $"WAVE {HordeManager.Instance.currentWaveIndex + 1}";
            return;
        }

        float time = HordeManager.Instance.remainingNextWaveTime;

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        timerText.text = $"NEXT WAVE IN {minutes:00}:{seconds:00}";

        timerText.color = time <= 5f
            ? Color.red
            : Color.white;
    }
}
