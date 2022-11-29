using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartUp : Singleton<StartUp>
{
    [SerializeField] UIManager ui;
    [SerializeField] GameManager gameManager;
    [SerializeField] AnalyticEvents analyticEvents;
    [SerializeField] FacebookManager facebook;
    [SerializeField] FirebaseManager firebase;
    [SerializeField] AdMob adMob;
    //[SerializeField] IronSourceManager ironSource;
    //[SerializeField] SoundManager soundManager;
    //[SerializeField] TenjinManager tenjin;

    private void Start()
	{
        Application.targetFrameRate = 60;

        firebase.Initialize();
        analyticEvents.Initialize();
        facebook.Initialize();

        if(gameManager == null) gameManager = GameManager.GetInstance();

        StartCoroutine(Initialize());
    }

    WaitForSeconds w = new WaitForSeconds(1.0f);

    IEnumerator Initialize()
    {
        yield return w;

        gameManager.Initialize();

        yield return new WaitUntil(() => AnalyticEvents.IsInitialized());

        adMob.Initialize(gameManager);
        yield return new WaitUntil(() => AdMob.IsInitialized);

        //TODO[CONSENT]: This is temp. remove when decided regarding the consent
        gameManager.AcceptConsent(true);

        int fetchConfigTimeout = 3;

        while(fetchConfigTimeout > 0 && !FirebaseManager.IsFetchedRemoteConfig)
        {
            yield return w;
            fetchConfigTimeout--;
        }

        ApplyRemoteConfig();

        /*if(SceneManager.sceneCount > 1)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync("Loader");

            while(unload.isDone)
            {
                yield return w;
            }
        }*/

        if(AdMob.IsInitialized)
        {
            AdMob.Instance.RequestAll();
        }
    }

    private void ApplyRemoteConfig()
    {
        // AdMob
        bool timeout10sec = FirebaseManager.GetRemoteConfigBoolean("Timeout_10sec");
        bool timeout30sec = FirebaseManager.GetRemoteConfigBoolean("Timeout_30sec");
        bool timeout45sec = FirebaseManager.GetRemoteConfigBoolean("Timeout_45sec");
        bool timeout60sec = FirebaseManager.GetRemoteConfigBoolean("Timeout_60sec");

        Debug.Log($"Firebase remote config: Timeout_10sec:{timeout10sec} Timeout_30sec:{timeout30sec} Timeout_60sec:{timeout60sec}");

        if(timeout60sec)
            AdMob.Instance.interstitialDelay = 60;
        else if(timeout45sec)
            AdMob.Instance.interstitialDelay = 45;
        else if(timeout30sec)
            AdMob.Instance.interstitialDelay = 30;
        else if(timeout10sec)
            AdMob.Instance.interstitialDelay = 10;

        //#if IRONSOURCE
        /*if (timeout60sec)
            IronSourceManager.Instance.interstitialDelay = 60;
        else if (timeout45sec)
            IronSourceManager.Instance.interstitialDelay = 45;
        else if (timeout30sec)
            IronSourceManager.Instance.interstitialDelay = 30;
        else if (timeout10sec)
            IronSourceManager.Instance.interstitialDelay = 10;*/

        Debug.Log($"Firebase remote config set ad timeout {AdMob.Instance.interstitialDelay}");
        FirebaseManager.SetCustomKey("interstitial_delay", AdMob.Instance.interstitialDelay.ToString());
        
       /* Debug.Log($"Firebase remote config set ad timeout {IronSourceManager.Instance.interstitialDelay}");
        FirebaseManager.SetCustomKey("interstitial_delay", IronSourceManager.Instance.interstitialDelay.ToString());*/
        //#endif

        // Disabled on current release
        //var internetRequired = FirebaseManager.GetRemoteConfigBoolean("no_internet");
        //
        //Debug.Log($"Firebase remote config: no_internet: {internetRequired}");
        //
        //
        //gameManager.internetRequired = internetRequired;

        // Internet reachability
        var internetRequired = FirebaseManager.GetRemoteConfigBoolean("no_internet");
        Debug.Log($"Firebase remote config: no_internet: {internetRequired}");
        FirebaseManager.SetCustomKey("internet_required", internetRequired.ToString());

        gameManager.internetRequired = internetRequired;
    }

    void OnApplicationFocus(bool focus)
    {
        if(AnalyticEvents.IsInitialized())
        {
            if(focus)
                AnalyticEvents.ReportEvent("foregroup_app");
            else
                AnalyticEvents.ReportEvent("background_app");
        }
    }
}
