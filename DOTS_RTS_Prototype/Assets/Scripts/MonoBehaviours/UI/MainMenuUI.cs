
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] Button playbutton;
    [SerializeField] Button quitbutton;

    void Awake()
    {
        playbutton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(1);
        });
        quitbutton.onClick.AddListener(() =>
        {
            Application.Quit();
        });
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
