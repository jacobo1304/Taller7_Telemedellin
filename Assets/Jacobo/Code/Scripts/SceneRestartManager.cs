using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRestartManager : MonoBehaviour
{
    public enum RestartMode
    {
        ReloadActiveScene,
        LoadFirstSceneInBuild
    }

    [SerializeField] private RestartMode restartMode = RestartMode.ReloadActiveScene;

    private bool isRestarting;

    public void RestartExperience()
    {
        if (isRestarting)
        {
            return;
        }

        StartCoroutine(RestartRoutine());
    }

    [ContextMenu("Restart Experience")]
    private void RestartExperienceContextMenu()
    {
        RestartExperience();
    }

    private IEnumerator RestartRoutine()
    {
        isRestarting = true;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        AudioListener.volume = 1f;

        int buildIndex;
        switch (restartMode)
        {
            case RestartMode.LoadFirstSceneInBuild:
                buildIndex = 0;
                break;
            default:
                Scene active = SceneManager.GetActiveScene();
                buildIndex = active.buildIndex;
                break;
        }

        AsyncOperation load;
        if (buildIndex >= 0)
        {
            load = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single);
        }
        else
        {
            load = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }

        while (!load.isDone)
        {
            yield return null;
        }
    }
}
