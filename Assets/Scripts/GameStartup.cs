using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameStartup : MonoBehaviour
{
    public Slider progressBar;
    public Text progressBarText;

    AsyncOperation operation;

    void Start()
    {
        StartCoroutine(LoadAsyncronously());
    }

    IEnumerator LoadAsyncronously()
    {
        yield return new WaitForSeconds(0.5f);

        operation = SceneManager.LoadSceneAsync("main");

        float progress = 0;

        while (!operation.isDone)
        {
            progress = Mathf.Clamp01(operation.progress * 0.9f);
            //progressBar.value = progress;
            //progressBarText.text = Mathf.Round(progress * 100) + "%";
            yield return null;
        }
    }
}

