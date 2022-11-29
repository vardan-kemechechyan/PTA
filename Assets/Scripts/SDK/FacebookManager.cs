using System.Collections.Generic;
using UnityEngine;
using Facebook.Unity;
using UnityEngine.UI;
using System;

public class FacebookManager : Singleton<FacebookManager>
{
    public bool IsInitialized()
    {
        // YsoCorp FB.Init()
        //   Debug.LogWarning("Facebook SDK not initialized yet!");

        return FB.IsInitialized;
    }

    public void Initialize()
    {
        // YsoCorp FB.Init()

        if (!FB.IsInitialized)
        {
            FB.Init(() =>
            {
                if (FB.IsInitialized) 
                {
                    FB.ActivateApp();
        
                    FB.Mobile.FetchDeferredAppLinkData(result =>
                    {
                        AppMetrica.Instance.ReportReferralUrl(result.TargetUrl);
                    });
        
                    FB.GetAppLink(result =>
                    {
                        AppMetrica.Instance.ReportAppOpen(result.TargetUrl);
        
                        FB.ClearAppLink();
                    });

                    Debug.Log("FB Init");
                }                 
                else
                    Debug.LogError("FB GetAppLink");
            },
            isGameShown =>
            {
                if (!isGameShown)
                    Time.timeScale = 0;
                else
                    Time.timeScale = 1;
            });
        }
        else
            FB.ActivateApp();
    }

    public void InitAfterYsocorp() 
    {
        FB.Mobile.FetchDeferredAppLinkData(result =>
        {
            AppMetrica.Instance.ReportReferralUrl(result.TargetUrl);
        });
        FB.GetAppLink(result =>
        {
            AppMetrica.Instance.ReportAppOpen(result.TargetUrl);

            FB.ClearAppLink();
        });
    }

    #region Login / Logout
    public void FacebookLoginSilently()
    {
        FB.Android.RetrieveLoginStatus(LoginStatusCallback);
    }

    private void LoginStatusCallback(ILoginStatusResult result)
    {
        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.Log("Error: " + result.Error);
        }
        else if (result.Failed)
        {
            Debug.Log("Failure: Access Token could not be retrieved");
        }
        else
        {
            // Successfully logged user in
            // A popup notification will appear that says "Logged in as <User Name>"
            Debug.Log("Success: " + result.AccessToken.UserId);
        }
    }

    public void FacebookLogin()
    {
        var permissions = new List<string>() { "public_profile", "email", "user_friends" };
        FB.LogInWithReadPermissions(permissions, AuthCallback);
    }

    private void AuthCallback(ILoginResult result)
    {
        if (FB.IsLoggedIn)
        {
            // AccessToken class will have session details
            var aToken = Facebook.Unity.AccessToken.CurrentAccessToken;
            // Print current access token's User ID
            Debug.Log(aToken.UserId);
            // Print current access token's granted permissions
            foreach (string perm in aToken.Permissions)
            {
                Debug.Log(perm);
            }
        }
        else
        {
            Debug.Log("User cancelled login");
        }
    }

    public void FacebookLogout()
    {
        FB.LogOut();
    }
    #endregion

    public void ReportEvent(string name) 
    {
        if (!IsInitialized()) return;

        FB.LogAppEvent(name);
    }

    public void ReportEvent(string name, Dictionary<string, object> parameters)
    {
        if (!IsInitialized()) return;

        FB.LogAppEvent(name, null, parameters);
    }

    public void Invite() 
    {
        if (!IsInitialized()) return;

        FB.AppRequest("Come play this great game!",null, null, null, null, null, null, delegate (IAppRequestResult result) 
        {
            Debug.Log(result.RawResult); 
        });
    }

    public void Share()
    {
        if (!IsInitialized()) return;

        FB.ShareLink(new Uri("https://developers.facebook.com/"), callback: ShareCallback);
    }

    private void ShareCallback(IShareResult result)
    {
        if (result.Cancelled || !String.IsNullOrEmpty(result.Error))
        {
            Debug.Log("ShareLink Error: " + result.Error);
        }
        else if (!String.IsNullOrEmpty(result.PostId))
        {
            // Print post identifier of the shared content
            Debug.Log(result.PostId);
        }
        else
        {
            // Share succeeded without postID
            Debug.Log("ShareLink success!");
        }
    }

    public void GetFriendsPlayingThisGame()
    {
        string query = "/me/friends";
        FB.API(query, HttpMethod.GET, result =>
        {
            var dictionary = (Dictionary<string, object>)Facebook.MiniJSON.Json.Deserialize(result.RawResult);
            var friendsList = (List<object>)dictionary["data"];
            /*
            FriendsText.text = string.Empty;
            foreach (var dict in friendsList)
                FriendsText.text += ((Dictionary<string, object>)dict)["name"];
            */
        });
    }
}
