using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UI;
using TMPro;

public class GameScreen : UIScreenController
{
    [SerializeField] Slider progressBar;
    [SerializeField] Text cash;
    [SerializeField] TextMeshProUGUI title;
    [SerializeField] Text rank;
    [SerializeField] GameObject playButton;
    [SerializeField] GameObject skipLevel;
    [SerializeField] GameObject storeButton;
    [SerializeField] GameObject levelInfo;
    [SerializeField] GameObject tutorial;
    [SerializeField] GameObject stageItemPrefab;

    [SerializeField] GameObject devSettings;
    [SerializeField] InputField[] cameraOffset;
    [SerializeField] InputField cameraSpeed;
    [SerializeField] TMP_InputField levelToSkip;
    [SerializeField] Image[] controlButtons;
    [SerializeField] Color[] controlButtonsColors;

    [SerializeField] bool showDevSettings;

    List<StageBarItem> stages = new List<StageBarItem>();

    public override void OnShow()
    {
        base.OnShow();

        skipLevel.SetActive(GameManager.GetInstance().TEST_MODE);

        title.GetComponentInChildren<TextMeshProUGUI>().text = "TOWN " + PlayerPrefs.GetInt("level");

        var offset = CameraController.GetInstance().customCameraOffset;

        cameraOffset[0].text = offset.x.ToString();
        cameraOffset[1].text = offset.y.ToString();
        cameraOffset[2].text = offset.z.ToString();

        cameraSpeed.text = CameraController.GetInstance().customChaseSpeed.ToString();

        ShowControlButtons(false);
    }

    public void SetTestLevel()
    {
        GameManager.GetInstance().SetTestLevel(levelToSkip.text);
    }

    public void Skip()
    {
        levelToSkip.text = "-1";
    }

    public void Play()
    {
        OnPlay();

        if (!PlayerPrefs.HasKey("tutorial"))
            tutorial.SetActive(true);
        else GameManager.GetInstance().StartStage();
    }

    public void SetStages() 
    {
        foreach(var s in stages)
            Destroy(s.gameObject);

        stages.Clear();

        foreach (var s in GameManager.GetInstance().level.stages) 
        {
            var stage = Instantiate(stageItemPrefab, stageItemPrefab.transform.parent).GetComponent<StageBarItem>();

            stages.Add(stage);
            stage.icon.color = GameManager.GetInstance().GetStageColor(s);

            stage.gameObject.SetActive(true);
        }
    }

    public void UpdateStages(int stagesCompleted) 
    {
        for (int i = 0; i < GameManager.GetInstance().level.stages.Length; i++) 
        {
            if (i < stagesCompleted)
            {
                stages[i].icon.gameObject.SetActive(true);
            }
            else 
            {
                stages[i].icon.gameObject.SetActive(false);
            }
        }
    }

    public void CompleteTutorial() 
    {
        if (tutorial.activeInHierarchy) 
        {
            PlayerPrefs.SetInt("tutorial", 1);
            PlayerPrefs.Save();

            tutorial.SetActive(false);

            GameManager.GetInstance().StartStage();
        }
    }

    public void ShowControlButtons(bool show) 
    {
        foreach (var b in controlButtons)
            b.gameObject.SetActive(show);
    }

    public void UpdateControlButtons(int dir) 
    {
        foreach (var b in controlButtons)
            b.color = controlButtonsColors[1];

        if(dir < 0) controlButtons[0].color = controlButtonsColors[0];
        else if (dir > 0) controlButtons[1].color = controlButtonsColors[0];
    }

    public void SetProgress(float progress) 
    {
        progressBar.value = progress;
    }

    public void ShowProgress(bool show)
    {
        //progressBar.gameObject.SetActive(show);
        progressBar.gameObject.SetActive(false); // Not implemented
    }

    public void OnStartStage() 
    {
        if (showDevSettings)
            devSettings.SetActive(true);

        playButton.GetComponentInChildren<TextMeshProUGUI>().text = "NEXT";
        playButton.SetActive(true);
    }

    public void OnSelect() 
    {
        if(showDevSettings)
            devSettings.SetActive(true);

        playButton.GetComponentInChildren<TextMeshProUGUI>().text = "PLAY";

        cash.gameObject.transform.parent.gameObject.SetActive(true);
        title.gameObject.SetActive(true);
        //rank.gameObject.transform.parent.gameObject.SetActive(true);
        rank.gameObject.transform.parent.gameObject.SetActive(false); // Not implemented
        playButton.SetActive(true);
        //storeButton.SetActive(true);
        storeButton.SetActive(false); // Not implemented

        //levelInfo.SetActive(true);
        levelInfo.SetActive(false); // Not implemented
    }

    public void OnPlay()
    {
        devSettings.SetActive(false);

        cash.gameObject.transform.parent.gameObject.SetActive(false);
        title.gameObject.SetActive(false);
        rank.gameObject.transform.parent.gameObject.SetActive(false);
        playButton.SetActive(false);
        storeButton.SetActive(false);
        levelInfo.SetActive(false);
    }

    public void SetCash(int value) 
    {
        cash.text = value.ToString();
    }

    public void SetCustomCameraOffset() 
    {
        float.TryParse(cameraOffset[0].text, out float x);
        float.TryParse(cameraOffset[1].text, out float y);
        float.TryParse(cameraOffset[2].text, out float z);

        CameraController.GetInstance().customCameraOffset = new Vector3(x, y, z);
    }

    public void SetCustomCameraChaseSpeed()
    {
        float.TryParse(cameraSpeed.text, out float s);

        CameraController.GetInstance().customChaseSpeed = s;
    }
}
