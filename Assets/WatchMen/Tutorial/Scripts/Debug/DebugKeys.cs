using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugKeys : MonoBehaviour
{
    void Update()
    {
        if (!(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) return;

        for (int i = 0; i < 7; i++)
            if (Input.GetKeyDown(KeyCode.F1 + i))
            {
                PlayerPrefs.SetInt("debugStage", i + 1);
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

        if (Input.GetKeyDown(KeyCode.F12))
        {
            PlayerPrefs.DeleteKey("debugStage");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}