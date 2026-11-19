using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused;

    public GameObject pausePanel;
    public VideoManager video;
    public string menuScene = "WatchmenMainMenu";

    bool wasA, wasB;

    void Start()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        pausePanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused) Resume(); else Pause();
        }
    }

    public void Pause()
    {
        IsPaused = true;
        wasA = video.playerA.isPlaying;
        wasB = video.playerB.isPlaying;
        if (wasA) video.playerA.Pause();
        if (wasB) video.playerB.Pause();
        Time.timeScale = 0f;
        AudioListener.pause = true;
        pausePanel.SetActive(true);
    }

    public void Resume()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (wasA) video.playerA.Play();
        if (wasB) video.playerB.Play();
        IsPaused = false;
    }

    public void Restart()
    {
        PlayerPrefs.DeleteKey("debugStage");   // remove this line when you add checkpoints
        Leave(SceneManager.GetActiveScene().name);
    }

    public void MainMenu() => Leave(menuScene);

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Leave(string scene)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
        SceneManager.LoadScene(scene);
    }
}