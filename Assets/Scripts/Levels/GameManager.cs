using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public void PlayScene()
    {
        SceneManager.LoadScene("Arena");
    }
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("Main Menu");
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void GoToSpecificScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene($"{sceneName}");
        }
        else
        {
            Debug.LogError($"Scene '{sceneName}' does not exist.");
        }
    }
}
