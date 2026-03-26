using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] TMP_Text messageText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DOTSEventManager.Instance.OnGameOver += DOTSEventManager_OnGameOver;
        SetVisible(false);
    }

    private void DOTSEventManager_OnGameOver(object sender, MsgEventArgs e)
    {
        SetVisible(true);
        Time.timeScale = 0f;

        if (e.msg != null)
        {
            messageText.text = e.msg;
        }
    }

    private void SetVisible(bool value)
    {
        gameObject.SetActive(value);
    }
}
