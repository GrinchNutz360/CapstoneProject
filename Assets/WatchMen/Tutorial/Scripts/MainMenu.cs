using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string gameScene = "TutorialScene";    // your tutorial scene's exact name

    public void Play()
    {
        PlayerPrefs.DeleteKey("debugStage");   // always start from the beginning
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(gameScene);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // Application.Quit does nothing in the editor
#else
        Application.Quit();
#endif
    }
}