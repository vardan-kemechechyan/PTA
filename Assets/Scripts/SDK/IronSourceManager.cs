using UnityEngine;
using System.Collections;
using System;
using GameAnalyticsSDK;
using System.Collections.Generic;

public class IronSourceManager : Singleton<IronSourceManager>
{
	[SerializeField] private string androidAppKey;
	[SerializeField] private string iosAppKey;

	static DateTime lastAdWatch;
	static DateTime lastRewardedAdWatch;

	public bool isRewarded;

	float elapsedTime;
	string gameAnalyticsSDKName = "ironsource";


	public bool IsAdOpen { get; private set; }

	public bool IsInterstitialLoaded()
	{
		return IronSource.Agent.isInterstitialReady();
	}

	public bool IsRewardedLoaded { get; private set; }

	public static event OnShowAd OnShow;
	public delegate void OnShowAd();

	public static event OnEarnReward OnRewarded;
	public delegate void OnEarnReward();

	public static event OnFailedRewarded OnRewardedFailed;
	public delegate void OnFailedRewarded();

	private string currentPlacement;

	/*protected override void Awake()
    {
        base.Awake();

#if UNITY_ANDROID
		string appKey = androidAppKey;
#elif UNITY_IPHONE
        string appKey = iosAppKey;
#else
		string appKey = "unexpected_platform";
#endif
		//Dynamic config example
		IronSourceConfig.Instance.setClientSideCallbacks(true);

		IronSource.Agent.setAdaptersDebug(true);

		string id = IronSource.Agent.getAdvertiserId();
		Debug.Log("IRONSOURCE Advertiser Id : " + id);

		Debug.Log("IRONSOURCE Validate integration...");
		IronSource.Agent.validateIntegration();

		Debug.Log("IRONSOURCE Unity version:" + IronSource.unityVersion());

		// Add Banner Events
		IronSourceEvents.onBannerAdLoadedEvent += BannerAdLoadedEvent;
		IronSourceEvents.onBannerAdLoadFailedEvent += BannerAdLoadFailedEvent;
		IronSourceEvents.onBannerAdClickedEvent += BannerAdClickedEvent;
		IronSourceEvents.onBannerAdScreenPresentedEvent += BannerAdScreenPresentedEvent;
		IronSourceEvents.onBannerAdScreenDismissedEvent += BannerAdScreenDismissedEvent;
		IronSourceEvents.onBannerAdLeftApplicationEvent += BannerAdLeftApplicationEvent;

		// Add Interstitial Events
		IronSourceEvents.onInterstitialAdReadyEvent += InterstitialAdReadyEvent;
		IronSourceEvents.onInterstitialAdLoadFailedEvent += InterstitialAdLoadFailedEvent;
		IronSourceEvents.onInterstitialAdShowSucceededEvent += InterstitialAdShowSucceededEvent;
		IronSourceEvents.onInterstitialAdShowFailedEvent += InterstitialAdShowFailedEvent;
		IronSourceEvents.onInterstitialAdClickedEvent += InterstitialAdClickedEvent;
		IronSourceEvents.onInterstitialAdOpenedEvent += InterstitialAdOpenedEvent;
		IronSourceEvents.onInterstitialAdClosedEvent += InterstitialAdClosedEvent;

		//Add Rewarded Video Events
		IronSourceEvents.onRewardedVideoAdOpenedEvent += RewardedVideoAdOpenedEvent;
		IronSourceEvents.onRewardedVideoAdClosedEvent += RewardedVideoAdClosedEvent;
		IronSourceEvents.onRewardedVideoAvailabilityChangedEvent += RewardedVideoAvailabilityChangedEvent;
		IronSourceEvents.onRewardedVideoAdStartedEvent += RewardedVideoAdStartedEvent;
		IronSourceEvents.onRewardedVideoAdEndedEvent += RewardedVideoAdEndedEvent;
		IronSourceEvents.onRewardedVideoAdRewardedEvent += RewardedVideoAdRewardedEvent;
		IronSourceEvents.onRewardedVideoAdShowFailedEvent += RewardedVideoAdShowFailedEvent;
		IronSourceEvents.onRewardedVideoAdClickedEvent += RewardedVideoAdClickedEvent;

		// SDK init
		Debug.Log("IRONSOURCE Init");
		//IronSource.Agent.init(appKey);
		IronSource.Agent.init(appKey, IronSourceAdUnits.REWARDED_VIDEO, IronSourceAdUnits.INTERSTITIAL, IronSourceAdUnits.BANNER);
		//IronSource.Agent.initISDemandOnly (appKey, IronSourceAdUnits.REWARDED_VIDEO, IronSourceAdUnits.INTERSTITIAL);

		// Set User ID For Server To Server Integration
		//IronSource.Agent.setUserId ("UserId");

		//IronSource.Agent.loadBanner(IronSourceBannerSize.BANNER, IronSourceBannerPosition.BOTTOM);
		IronSource.Agent.loadInterstitial();

		#if UNITY_ANDROID && !UNITY_EDITOR
				AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
				AndroidJavaClass client = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
				AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);
		
				//advertisingIdClient.text = adInfo.Call<string>("getId").ToString();
				Debug.Log($"IRONSOURCE Android advertising ID: {adInfo.Call<string>("getId").ToString()}");
		#endif
		
		#if UNITY_IOS && !UNITY_EDITOR
				Application.RequestAdvertisingIdentifierAsync((string advertisingId, bool trackingEnabled, string error) =>
				{
					//advertisingIdClient.text = advertisingId;
					Debug.Log($"IRONSOURCE iOS advertising ID: {advertisingId}");
				});
		#endif
	}*/

	/*void OnApplicationPause(bool paused)
	{
		Debug.Log("IRONSOURCE OnApplicationPause = " + paused);
		IronSource.Agent.onApplicationPause(paused);

		if (paused)
		{
			if (currentPlacement != null)
			{
				GameAnalytics.PauseTimer(currentPlacement);
			}
		}
		else
		{
			if (currentPlacement != null)
			{
				GameAnalytics.ResumeTimer(currentPlacement);
			}
		}
	}*/

	public void DestroyAd()
	{
		IronSource.Agent.destroyBanner();
	}

	public void ShowBanner()
	{
		IronSource.Agent.displayBanner();
	}

	public void HideBanner()
	{
		IronSource.Agent.hideBanner();
	}

	bool isLoadingInterstitial;

	public void RequestInterstitial()
	{
		if (!IronSource.Agent.isInterstitialReady())
		{
			OnShow?.Invoke();

			isLoadingInterstitial = true;

			IronSource.Agent.loadInterstitial();
		}
	}

	public void ShowInterstitial()
	{
		currentPlacement = "";

		if (IronSource.Agent.isInterstitialReady())
		{
			IronSource.Agent.showInterstitial();
		}
		else
		{
			RequestInterstitial();
		}
	}

	public void ShowRewardedVideo(string placement)
	{
		currentPlacement = placement;

		if (IronSource.Agent.isRewardedVideoAvailable())
		{
			OnShow?.Invoke();

			IronSource.Agent.showRewardedVideo();
		}
	}

	// Banner
	void BannerAdLoadedEvent()
	{
		Debug.Log("IRONSOURCE BannerAdLoadedEvent");

		GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Banner, gameAnalyticsSDKName, currentPlacement);

		AnalyticEvents.ReportEvent("banner_bottom");
	}

	void BannerAdLoadFailedEvent(IronSourceError error)
	{
		Debug.Log("IRONSOURCE BannerAdLoadFailedEvent, code: " + error.getCode() + ", description : " + error.getDescription());
	}

	void BannerAdClickedEvent()
	{
		GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType.Banner, gameAnalyticsSDKName, currentPlacement);

		Debug.Log("IRONSOURCE BannerAdClickedEvent");
	}

	void BannerAdScreenPresentedEvent()
	{
		Debug.Log("IRONSOURCE BannerAdScreenPresentedEvent");
	}

	void BannerAdScreenDismissedEvent()
	{
		Debug.Log("IRONSOURCE BannerAdScreenDismissedEvent");
	}

	void BannerAdLeftApplicationEvent()
	{
		Debug.Log("IRONSOURCE BannerAdLeftApplicationEvent");
	}

	// Iterstitial
	void InterstitialAdReadyEvent()
	{
		Debug.Log("IRONSOURCE InterstitialAdReadyEvent");
	}

	void InterstitialAdLoadFailedEvent(IronSourceError error)
	{
		Debug.Log("IRONSOURCE InterstitialAdLoadFailedEvent, code: " + error.getCode() + ", description : " + error.getDescription());
	}

	void InterstitialAdShowSucceededEvent()
	{
		IsAdOpen = true;

		GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Interstitial, gameAnalyticsSDKName, currentPlacement);

		Debug.Log("IRONSOURCE InterstitialAdShowSucceededEvent");
	}

	void InterstitialAdShowFailedEvent(IronSourceError error)
	{
		IsAdOpen = false;

		GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Interstitial, gameAnalyticsSDKName, currentPlacement);

		Debug.Log("IRONSOURCE InterstitialAdShowFailedEvent, code :  " + error.getCode() + ", description : " + error.getDescription());
	}

	void InterstitialAdClickedEvent()
	{
		GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType.Interstitial, gameAnalyticsSDKName, currentPlacement);

		Debug.Log("IRONSOURCE InterstitialAdClickedEvent");
	}

	void InterstitialAdOpenedEvent()
	{
		Debug.Log("IRONSOURCE InterstitialAdOpenedEvent");

		RequestInterstitial();

		var lvl = new Dictionary<string, object>();
		lvl.Add("level", PlayerPrefs.GetInt("level"));

		AnalyticEvents.ReportEvent("Interstitial_lvl", lvl);
	}

	void InterstitialAdClosedEvent()
	{
		IsAdOpen = false;

		Debug.Log("IRONSOURCE InterstitialAdClosedEvent");
	}

	// Rewarded
	void RewardedVideoAvailabilityChangedEvent(bool canShowAd)
	{
		IsRewardedLoaded = canShowAd;

		Debug.Log("IRONSOURCE RewardedVideoAvailabilityChangedEvent, value = " + canShowAd);
	}

	void RewardedVideoAdOpenedEvent()
	{
		IsAdOpen = true;

		GameAnalytics.StartTimer(currentPlacement);

		Debug.Log("IRONSOURCE RewardedVideoAdOpenedEvent");
	}

	void RewardedVideoAdRewardedEvent(IronSourcePlacement ssp)
	{
		Debug.Log("IRONSOURCE RewardedVideoAdRewardedEvent, amount = " + ssp.getRewardAmount() + " name = " + ssp.getRewardName());

		GameAnalytics.NewAdEvent(GAAdAction.RewardReceived, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement);

		ReportRevenue(ssp);
		
		OnRewarded.Invoke();
	}

	void RewardedVideoAdClosedEvent()
	{
		IsAdOpen = false;

		long elapsedTime = GameAnalytics.StopTimer(currentPlacement);

		GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement, elapsedTime);

		Debug.Log("IRONSOURCE RewardedVideoAdClosedEvent");
	}

	void RewardedVideoAdStartedEvent()
	{
		Debug.Log("IRONSOURCE RewardedVideoAdStartedEvent");
	}

	void RewardedVideoAdEndedEvent()
	{
		Debug.Log("IRONSOURCE RewardedVideoAdEndedEvent");
	}

	void RewardedVideoAdShowFailedEvent(IronSourceError error)
	{
		OnRewardedFailed.Invoke();

		IsAdOpen = false;

		GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, gameAnalyticsSDKName, currentPlacement);

		Debug.Log("IRONSOURCE RewardedVideoAdShowFailedEvent, code :  " + error.getCode() + ", description : " + error.getDescription());
	}

	void RewardedVideoAdClickedEvent(IronSourcePlacement ssp)
	{
		Debug.Log("IRONSOURCE RewardedVideoAdClickedEvent, name = " + ssp.getRewardName());
	}
	
	private void ReportRevenue(IronSourcePlacement placement)
	{
		var revenue = new YandexAppMetricaRevenue((decimal)(placement.getRewardAmount() / 1000000f), placement.getRewardName());
            
		revenue.ProductID = placement.getPlacementName();
		revenue.Receipt = new YandexAppMetricaReceipt();

		AppMetrica.Instance.ReportRevenue(revenue);
		FirebaseManager.ReportRevenue(placement.getPlacementName(), placement.getRewardAmount() / 1000000f, placement.getRewardName());
            
		Debug.Log($"Report revenue AdUnit: {placement.getPlacementName()} Value: {placement.getRewardAmount() / 1000000f} Currency: {placement.getRewardName()}");
	}
}
