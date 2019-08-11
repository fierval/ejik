using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameSettings gameSettings;

    private void Start()
    {
        gameSettings.Initialize();
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void About()
    {
        SceneManager.LoadScene("About");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void Sound()
    {
        gameSettings.ToggleSound();
    }
}
