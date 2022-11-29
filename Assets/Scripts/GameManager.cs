using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using VehicleBehaviour;
using UI;

public class GameManager : Singleton<GameManager>
{
    public bool TEST_MODE;

    public bool internetRequired;

    [SerializeField] GameObject splashScreen;

    [SerializeField] bool clearPrefsInEditor;
    public int testLevel = 1;

    public Level level;
    public Level.Stage stage;

    public WheelVehicle[] ghosts;
    public WheelVehicle player;
    List<CarTrailDrawer> GhostTrailRenderers;
    public bool IsTrailRenderingWhileDriving = false;

    [ SerializeField] EndStageMessage endStageMessage;

    [SerializeField] Light mainLight;
    public Light MainLight { get => mainLight; }

    [Header("Settings")]
    [SerializeField] bool dayNightCicle;
    [SerializeField] Color[] stageColors;
    [SerializeField] string[] levelCompleteMessages;
    [SerializeField] string[] levelFailedMessages;

    [Header("Player data")]

    [SerializeField] int playerCash;
    public int PlayerCash 
    {
        get => playerCash;
        set 
        {
            if (value < 0)
                value = 0;

            if(value != playerCash) 
            {
                playerCash = value;

                PlayerPrefs.SetInt("cash", playerCash);
                PlayerPrefs.Save();
            }

            gameScreen.SetCash(playerCash);
        }
    }

    GameScreen gameScreen;

    [SerializeField] float crashDelay = 2.0f;
    [SerializeField] float stageDelay = 3.0f;

    float levelProgress;
    public float LevelProgress 
    {
        get => levelProgress;
        set 
        {
            gameScreen.SetProgress(value);

            levelProgress = value;
        }
    }

    int levelReward;

    int stagesCompleted;

    public List<WheelVehicle.Track> tracks = new List<WheelVehicle.Track>();

    [SerializeField] CameraController camera;

    Obstacle[] levelObstacles;

    List<Trigger> parkingPonits = new List<Trigger>();

    //TODO Soundmanager
    public AudioClip[] objectCollisionSounds;

    [SerializeField] AudioClip clickSound;
    [SerializeField] AudioClip endLevelSound;

    [SerializeField] AudioSource uiAduio;

    bool IsThisFirstLaunchAfterInstall = true;

    [SerializeField] int currentLevel = 1;
    public int CurrentLevel 
    {
        get => currentLevel;
        set 
        {
            if (value < 1)
                value = 1;

            if (currentLevel != value) 
            {
                PlayerPrefs.SetInt("level", value);
                PlayerPrefs.Save();
            }

            currentLevel = value;
        }
    }

    public Color GetStageColor(Level.Stage stage) 
    {
        return stageColors[(int)stage.color];
    }

    public bool IsTurning { get; set; }

    public bool IsInputEnabled { get; private set; }

    public int GetStageIndex(Level.Stage stage) 
    {
        return Array.IndexOf(level.stages, stage);
    }

    public string GetEndStageMessage(bool success) 
    {
        if (success) return levelCompleteMessages[UnityEngine.Random.Range(0, levelCompleteMessages.Length - 1)];
        else return levelFailedMessages[UnityEngine.Random.Range(0, levelFailedMessages.Length - 1)];
    }

    [SerializeField] float dayCicleSpeed = 1.0f;
    [SerializeField] float dayLightIntensity = 1.0f;
    [SerializeField] float eveningLightIntensity = 0.35f;
    [SerializeField] float nightLightIntensity = 0.1f;

    [SerializeField] bool isPlay;
    public bool IsPlay 
    { 
        get => isPlay;
        private set 
        {
            if (value != isPlay)
                gameScreen.ShowControlButtons(value);

            isPlay = value;
        } 
    }

    protected override void Awake()
    {
        base.Awake();
    }

    public void Initialize()
    {
#if UNITY_EDITOR
        if (clearPrefsInEditor)
            PlayerPrefs.DeleteAll();
#endif

        CurrentLevel = PlayerPrefs.GetInt("level");

        /*if ( PlayerPrefs.HasKey( "FirstLaunchAfterInstall" ) )
        {
            if ( CurrentLevel == 1 && PlayerPrefs.GetInt( "FirstLaunchAfterInstall" ) == 0 )
            {
                PlayerPrefs.SetInt( "FirstLaunchAfterInstall", 1 );

                AnalyticEvents.ReportEvent( "install_app " );
            }
        }
        else
        {
            if ( CurrentLevel == 1 )
            {
                PlayerPrefs.SetInt( "FirstLaunchAfterInstall", 1 );

                AnalyticEvents.ReportEvent( "install_app " );
            }
        }*/

        gameScreen = (GameScreen)UIManager.GetInstance().GetScreen("game");

        PlayerCash = PlayerPrefs.GetInt("cash");

        //Application.targetFrameRate = 60;

        LoadLevel(CurrentLevel);

        SceneManager.sceneLoaded += OnSceneLoaded;

        crashStageDelay = new WaitForSeconds(crashDelay);
        endStageDelay = new WaitForSeconds(stageDelay);

        //AnalyticEvents.ReportEvent( "start_app" );
    }

    int nextLevelValue = 0;

    public void SetTestLevel( string _levelToLoadString )
    {
        int.TryParse(_levelToLoadString, out nextLevelValue);
    }

    public void SkipLevel()
    {
        if(nextLevelValue == -1)
            testLevel = CurrentLevel + 1;
        else
            testLevel = Mathf.Clamp(nextLevelValue, 1, 29);

        LoadLevel(testLevel);

        CurrentLevel = testLevel;

        nextLevelValue = -1;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            LoadLevel(testLevel);
        }

        if (dayNightCicle) 
        {
            if (isPlay)
            {
                mainLight.intensity = Mathf.Lerp(mainLight.intensity, GetLightIntensity(level.timeOfDay != Level.TimeOfDay.Night ? Level.TimeOfDay.Night : Level.TimeOfDay.Day), level.changeLightSpeed * dayCicleSpeed);
            }
        }
    }

    private float GetLightIntensity(Level.TimeOfDay timeOfDay) 
    {
        if (dayNightCicle)
        {
            switch (timeOfDay)
            {
                case Level.TimeOfDay.Day:
                    return dayLightIntensity;
                case Level.TimeOfDay.Evening:
                    return eveningLightIntensity;
                case Level.TimeOfDay.Night:
                    return nightLightIntensity;
                default:
                    return dayLightIntensity;
            }
        }
        else 
        {
            return dayLightIntensity;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (splashScreen.activeInHierarchy)
            splashScreen.SetActive(false);

        parkingPonits?.Clear();

        levelObstacles = FindObjectsOfType<Obstacle>();
        level = FindObjectOfType<Level>();

        foreach (var o in levelObstacles)
            o.Setup();

        if (!level) return;

        ghosts = new WheelVehicle[level.stages.Length - 1];

        var obj = GameObject.FindGameObjectsWithTag("Finish");

        foreach (var p in obj)
            parkingPonits.Add(p.GetComponent<Trigger>());

        for (int i = 0; i < parkingPonits.Count; i++) 
        {
            parkingPonits[i].GetComponent<Parking>().SetColor(stageColors[(int)level.stages[parkingPonits[i].id - 1].color]);
        }

        gameScreen.SetStages();

        SetStage(level.stages[0]);

        UIManager.GetInstance().ShowScreen("game");
        gameScreen.OnSelect();
    }

    public static bool IsLeveExist(int level) 
    {
        return IsSceneExist($"Level{level}");
    }

    public static bool IsSceneExist(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            var lastSlash = scenePath.LastIndexOf("/");
            var sceneName = scenePath.Substring(lastSlash + 1, scenePath.LastIndexOf(".") - lastSlash - 1);

            if (string.Compare(name, sceneName, true) == 0)
                return true;
        }

        return false;
    }

    public void SetStage(Level.Stage stage)
    {
        GhostTrailRenderers = new List<CarTrailDrawer>();
        GhostTrailRenderers = level.ReturnAllTrail();

        if (stage == level.stages[0])
        {
            stagesCompleted = 0;

            //if level stage is 0, hide al trails
            foreach ( var trail in GhostTrailRenderers )
                trail.SetTrailEnabled( false );
        }
        else
        {
            //Showing trail color for ghosts
		    for ( int i = GetStageIndex( stage ) - 1; i >= 0; i-- )
            {
                GhostTrailRenderers[ i ].SetTrailEnabled( true );
                GhostTrailRenderers[ i ].SetTrailRendererMaterialColor( stageColors[ ( int )level.stages[ i ].color ] );
            }
		}

        mainLight.intensity = GetLightIntensity(level.timeOfDay);

        camera.SetStartAnchor(stage.startCamera);

        UIManager.GetInstance().ShowScreen("game");      

        this.stage = stage;

        gameScreen.UpdateStages(stagesCompleted);

        if (level.traffic != null) 
        {
            foreach (var t in level.traffic)
            {
                t.gameObject.SetActive(false);
                t.ResetToStartPosition();
                t.ResetPath();
            }

            if (level.trafficMode == Level.TrafficMode.Random)
            {
                foreach (var t in level.traffic)
                    t.gameObject.SetActive(true);
            }
        }

        if (player)
            Destroy(player.gameObject);

        player = Instantiate(stage.vechicle).GetComponent<WheelVehicle>();

        camera.SetTarget(player);

        player.control = WheelVehicle.Control.Player;

        IsInputEnabled = false;
        player.Move = false;

        player.CrashSmoke(false);

        camera.State = CameraController.CameraState.Start;

        player.transform.position = stage.start.position;
        player.transform.rotation = stage.start.rotation;

        CarTrailDrawer PlayerTrail = stage.start.GetComponent<CarTrailDrawer>();
        
        //TODO: enable the script below to draw the line "live" while driving
        PlayerTrail.SetTrailEnabled( IsTrailRenderingWhileDriving );
        PlayerTrail.SetTrailRendererNewPosition( stage.start.position );

        player.CarTrail = PlayerTrail;

        player.CarTrail.ResetPositions();

        player.ClearParticles();

        var stageIndex = Array.IndexOf(level.stages, stage);

        foreach (var g in ghosts)
            if (g && g.gameObject)
                Destroy(g.gameObject);

        if (level.traffic != null)
        {
            if (level.trafficMode == Level.TrafficMode.Random)
            {
                foreach (var t in level.traffic)
                    t.Move = false;
            }
        }

        for (int i = 0; i < stageIndex; i++) 
        {
            ghosts[i] = Instantiate(level.stages[i].vechicle).GetComponent<WheelVehicle>();

            ghosts[ i ].control = WheelVehicle.Control.Ghost;

            ghosts[i].SetColor(stageColors[(int)level.stages[i].color]);

            ghosts[i].transform.position = tracks[i].moves[0].position;
            ghosts[i].transform.rotation = tracks[i].moves[0].rotation;

            ghosts[i].Move = false;
            ghosts[i].CurrentTrack = tracks[i];
            ghosts[i].gameObject.SetActive(true);

            player.ClearParticles();
        }

        foreach (var o in levelObstacles)
            o.ResetObstacle();

        player.SetTarget(parkingPonits.FirstOrDefault(x => x.id == stage.finish).transform.parent);

        foreach (var p in parkingPonits)
        {
            if (p.id == stage.finish) p.transform.parent.gameObject.SetActive(true);
            else p.transform.parent.gameObject.SetActive(false);
        }

        ShowPath(true);

        player.SetColor(stageColors[(int)stage.color]);
    }

    private void ShowPath(bool show) 
    {
        foreach (var p in parkingPonits)
            p.ShowPath(false);
        
        parkingPonits.FirstOrDefault(x => x.id == stage.finish).ShowPath(show);
    }

    public void StartStage()
    {
        if (!IsPlay)
        {
            StopCoroutine("StartStageCoroutine");
            StartCoroutine("StartStageCoroutine");
        }
    }

    IEnumerator StartStageCoroutine() 
    {
        if (stage == level.stages[0]) 
        {
            var lvl = new Dictionary<string, object>();
            lvl.Add("level", CurrentLevel);

            AnalyticEvents.ReportEvent($"lvl_start", lvl);
        }

        foreach ( var trail in GhostTrailRenderers )
            trail.SetTrailEnabled( false );

        player.CarTrail.SetTrailEnabled( IsTrailRenderingWhileDriving );

        camera.State = CameraController.CameraState.Chase;

        player.OnStartStage();

        yield return new WaitUntil(() => !camera.IsSnapingToAnchor);

        if (level.traffic != null) 
        {
            if (level.trafficMode == Level.TrafficMode.Random)
            {
                foreach (var t in level.traffic)
                    t.Move = true;
            }
        }

        player.Move = true;

        foreach (var ghost in ghosts)
        {
            if (ghost && ghost.gameObject.activeInHierarchy)
                ghost.Move = true;
        }

        IsInputEnabled = true;
        IsPlay = true;

        gameScreen.ShowProgress(true);
    }

    public void CompleteStage() 
    {
        CameraController.GetInstance().CameraShake(1.0f, CameraController.CameraShakeMode.Strong);

        endStageMessage.Play(GetEndStageMessage(true));

        stagesCompleted++;

        ShowPath(false);

        IsPlay = false;

        gameScreen.ShowProgress(false);

        IsInputEnabled = false;

        if(Array.IndexOf(level.stages, stage) < level.stages.Length - 1) 
            tracks.Add(player.CurrentTrack);

        camera.State = CameraController.CameraState.Chase;
        player.Move = false;

        gameScreen.UpdateStages(stagesCompleted);

        StopCoroutine("NextStage");
        StartCoroutine("NextStage");
    }

    public void PlayerToast(string text) 
    {
        endStageMessage.Play(text);
    }

    public void Crash()
    {
        if (IsPlay) 
        {
            CameraController.GetInstance().CameraShake(1.0f, CameraController.CameraShakeMode.Strong);

            PlayerToast(GetEndStageMessage(false));

            ShowPath(false);

            StopCoroutine("CrashCoroutine");
            StartCoroutine("CrashCoroutine");
        }
    }

    WaitForSeconds crashStageDelay;

    IEnumerator CrashCoroutine() 
    {
        IsPlay = false;

        gameScreen.ShowProgress(false);

        IsInputEnabled = false;

        player.Move = false;

        player.CrashSmoke(true);

        yield return crashStageDelay;

        camera.State = CameraController.CameraState.Crash;
        UIManager.GetInstance().ShowScreen("fail");
    }

    WaitForSeconds endStageDelay;

    public void Offroad()
    {
        if (IsPlay) 
        {
            ShowPath(false);

            StopCoroutine("OffroadCoroutine");
            StartCoroutine("OffroadCoroutine");
        }
    }

    IEnumerator OffroadCoroutine() 
    {
        IsPlay = false;
        IsInputEnabled = false;
        camera.State = CameraController.CameraState.None;
        yield return endStageDelay;
        player.Move = false;
        UIManager.GetInstance().ShowScreen("fail");
    }

    IEnumerator NextStage() 
    {
        yield return endStageDelay;

        int index = GetStageIndex(stage) + 1;

        if (index < level.stages.Length && level.stages[index] != null)
        {
            gameScreen.OnStartStage();
            SetStage(level.stages[index]);
        }
        else 
        {
            uiAduio.clip = endLevelSound;
            uiAduio.Play();

            var lvl = new Dictionary<string, object>();
            lvl.Add("level", CurrentLevel);

            AnalyticEvents.ReportEvent($"lvl_end", lvl);

            UIManager.GetInstance().ShowScreen("complete");
        }
    }

    public int GetReward(bool bonus) 
    {
        int reward = 5 * level.stages.Length;
        return bonus ? reward * 3 : reward;
    }

    public void RestartStage() 
    {
        tracks.Clear();
        SetStage(level.stages[0]);
        gameScreen.OnSelect();
    }

    public void RetryStage() 
    {
        gameScreen.OnStartStage();
        SetStage(GetInstance().stage);
    }

    public void NextLevel() 
    {
        gameScreen.OnSelect();

        tracks.Clear();

        CurrentLevel++;

        LoadLevel(CurrentLevel);
    }

    public void PlayClick() 
    {
        uiAduio.clip = clickSound;
        uiAduio.Play();
    }

    private void LoadLevel(int level) 
    {
        if(AdMob.IsInitialized)
        {
            AdMob.Instance.RequestAll();
        }

        if (!IsLeveExist(level))
            level = 1;

        SceneManager.LoadSceneAsync($"Level{level}", LoadSceneMode.Single);
    }

    public void Steer(int input) 
    {
        gameScreen.UpdateControlButtons(input);
    }

    public void AcceptConsent(bool accept)
    {
        PlayerPrefs.SetInt("consent", Convert.ToInt16(accept));
        PlayerPrefs.Save();

        //IronSourceManager.Instance.SetConsent(accept);

        /*
        var parameters = new Dictionary<string, object>();
        parameters.Add("accept", accept);
        AnalyticEvents.ReportEvent("persadspopup_request", parameters);
        */
    }
}
