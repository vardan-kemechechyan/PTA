#if GAMEANALYTICS
using GameAnalyticsSDK;
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnalyticEvents : Singleton<AnalyticEvents>
{
    public void Initialize()
    {
        StartCoroutine("InitializeCoroutine");
    }

    IEnumerator InitializeCoroutine()
    {
#if GAMEANALYTICS
                GameAnalytics.Initialize();
#endif

        yield return new WaitUntil(() => IsInitialized() == true);

        Debug.Log("Initialized analytics SDK");

        if(!PlayerPrefs.HasKey("initialLaunch"))
        {
            PlayerPrefs.SetInt("initialLaunch", 1);
            PlayerPrefs.Save();

            ReportEvent("install_app");
        }
        else
        {
            ReportEvent("start_app");
        }
    }

    public static bool IsInitialized()
    {
#if FACEBOOK
        if (!FacebookManager.IsInitialized())
            return false;
#endif

        if(!FirebaseManager.IsInitialized)
            return false;

        return true;
    }

    public static void ReportEvent(string name)
    {
        //TenjinManager.ReportEvent(name);
        FirebaseManager.ReportEvent(name);

#if FACEBOOK
        FacebookManager.ReportEvent(name);
#endif

        AppMetrica.Instance?.ReportEvent(name);

#if GAMEANALYTICS
        GameAnalytics.NewDesignEvent(name);
#endif
        Debug.Log($"Report event: {name}");
    }

    public static void ReportEvent(string name, Dictionary<string, object> parameters)
    {
        FirebaseManager.ReportEvent(name, parameters);

#if FACEBOOK
        FacebookManager.ReportEvent(name);
#endif

        AppMetrica.Instance?.ReportEvent(name, parameters);

#if GAMEANALYTICS
        GameAnalytics.NewDesignEvent(name);
#endif

        string str = "( ";

        foreach(var p in parameters)
            str += $" {p.Key} = {p.Value} ";

        str += " )";

        Debug.Log($"Report event: {name} {str}");
    }
}
