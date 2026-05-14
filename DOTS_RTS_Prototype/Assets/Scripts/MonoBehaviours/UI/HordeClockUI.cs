using TMPro;
using UnityEngine;

public class HordeWaveUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI subText;

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
            $"Eliminate all remaining orcs";
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
