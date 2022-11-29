using System;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using GoogleMobileAds.Api;

public class AdMob : MonoBehaviour
{
    public static AdMob Instance { get; private set; }

    [SerializeField] bool isTest;

    [SerializeField] AdPlacement[] placements;



    public bool skipInterstitial;

    public static event OnShowAd OnShow;
    public delegate void OnShowAd();

    public static event OnShowInterstitial OnInterstitial;
    public delegate void OnShowInterstitial();

    public static event OnShowInterstitialFailed OnInterstitialFailed;
    public delegate void OnShowInterstitialFailed();

    public static event OnEarnReward OnRewarded;
    public delegate void OnEarnReward();

    public static event OnFailedRewarded OnRewardedFailed;
    public delegate void OnFailedRewarded();

    public static bool IsInitialized { get; private set; }

    bool muteMusicOnShowAd;

    public DateTime LastAdWatch  { get; private set; }
    public DateTime LastRewardedAdWatch { get; private set; }

    public int interstitialDelay;
    public int rewardedDelay;

    bool isBannerActive;

    GameManager _gameManager;

    private AdPlacement GetPlacement(string name) 
    {
        return placements.FirstOrDefault(x => x.name.Equals(name));
    }

    public bool IsReady(string placement)
    {
        var p = GetPlacement(placement);

        if (p == null)
        {
            Debug.LogWarning($"AdMob Placement {placement} not found!");
            return false;
        }
        else 
        {
            if(_gameManager.CurrentLevel - 1 < p.completeLevelsToShow)
                return false;

            switch (p.type)
            {
                case AdPlacement.Type.Banner:
                    return p.banner != null;
                case AdPlacement.Type.Interstitial:
                    if ((DateTime.Now - LastAdWatch).TotalSeconds < interstitialDelay)
                    {
                        return false;
                    }
                    else
                    {
                        return p.interstitial != null ? p.interstitial.IsLoaded() : false;
                    }
                case AdPlacement.Type.Rewarded:
                    if ((DateTime.Now - LastRewardedAdWatch).TotalSeconds < rewardedDelay)
                    {
                        return false;
                    }
                    else
                    {
                        return p.rewarded != null ? p.rewarded.IsLoaded() : false;
                    }
            }

            return false;
        }
    }

    public void Initialize(GameManager gameManager)
    {
        Instance = this;

        _gameManager = gameManager;

        LastAdWatch = DateTime.Now.AddSeconds(-interstitialDelay);
        LastRewardedAdWatch = DateTime.Now.AddSeconds(-rewardedDelay);

        if (IsInitialized == false)
        {
            MobileAds.Initialize(initStatus =>
            {
                IsInitialized = true;

                RequestAll();
            });
        }
    }

    public void RequestAll()
    {
        foreach (var p in placements)
            Request(p.name);
    }

    public void Request(string placement)
    {
        var p = GetPlacement(placement);

        if (p == null)
        {
            Debug.LogWarning($"AdMob Placement {placement} not found!");
        }
        else
        {
            if (IsReady(placement))
                return;

            AdRequest request = new AdRequest.Builder().Build();

            switch (p.type)
            {
                case AdPlacement.Type.Banner:
                    //if (p.banner == null)
                    //{
                    //    if (isTest)
                    //        p.banner = new BannerView("ca-app-pub-3940256099942544/6300978111", AdSize.Banner, AdPosition.Bottom);
                    //    else
                    //        p.banner = new BannerView(p.Id, AdSize.Banner, AdPosition.Bottom);
                    //
                    //    p.banner.OnAdLoaded += (sender, args) => 
                    //    {
                    //        Debug.Log($"AdMob OnAdLoaded: {p.name}");
                    //        AnalyticEvents.ReportEvent("Banner_Bottom");
                    //    };
                    //
                    //    p.banner.OnAdFailedToLoad += (sender, args) => 
                    //    {
                    //        Debug.LogWarning($"AdMob OnAdFailedToLoad: {p.name} Error: {args.LoadAdError.GetMessage()}");
                    //    };
                    //
                    //    p.banner.OnAdOpening += (sender, args) => 
                    //    {
                    //        Debug.Log($"AdMob OnAdOpening: {p.name}");
                    //    };
                    //
                    //    p.banner.OnAdClosed += (sender, args) => 
                    //    {
                    //        Debug.Log($"AdMob OnAdClosed: {p.name}");
                    //    };
                    //
                    //    p.banner.OnPaidEvent += (sender, args) =>
                    //    {
                    //        Debug.Log($"AdMob OnPaid: {p.name} {args.AdValue.Value} {args.AdValue.CurrencyCode}");
                    //
                    //        paidAdUnit = currentPlacement.Id;
                    //        paidAdValue = args.AdValue;
                    //
                    //        isPaid = true;
                    //    };
                    //}
                    //
                    //p.banner.LoadAd(request);
                    break;
                case AdPlacement.Type.Interstitial:
                    if (p.interstitial == null)
                    {
                        if (isTest)
                            p.interstitial = new InterstitialAd("ca-app-pub-3940256099942544/1033173712");
                        else
                            p.interstitial = new InterstitialAd(p.Id);

                        p.interstitial.OnAdLoaded += (sender, args) => 
                        {
                            Debug.Log("OnOnInterstitialLoaded");
                        };

                        p.interstitial.OnAdFailedToLoad += (sender, args) =>
                        {
                            Debug.LogWarning($"AdMob OnAdFailedToLoad: {p.name} Error: {args.LoadAdError.GetMessage()}");
                            isInterstitialFailed = true;
                        };

                        p.interstitial.OnAdFailedToShow += (sender, args) =>
                        {
                            Debug.LogWarning($"AdMob OnAdFailedToShow: {p.name} Error: {args.AdError.GetMessage()}");
                            isInterstitialFailed = true;
                        };

                        p.interstitial.OnAdOpening += (sender, args) =>
                        {
                            Debug.Log($"AdMob OnAdOpening: {p.name}");

                            LastAdWatch = DateTime.Now;

                            /*var lvl = new Dictionary<string, object>();
                            lvl.Add("level", _gameManager.CurrentLevelIndex - 1);

                            AnalyticEvents.ReportEvent("Interstitial", lvl);

                            if (p.name.Equals("Interstitial_restart"))
                            {
                                var parameters = new Dictionary<string, object>();
                                parameters.Add("location", _gameManager.Location.id);
                                parameters.Add("level", _gameManager.Level + 1);

                                AnalyticEvents.ReportEvent("restart_interstitial", parameters);
                            }*/

                            if(p.Equals("Interstitial_restart"))
                            {
                                var parameters = new Dictionary<string, object>();
                                parameters.Add("level", _gameManager.CurrentLevel - 1);

                                AnalyticEvents.ReportEvent("restart_interstitial", parameters);
                            }
                            else
                            {
                                var lvl = new Dictionary<string, object>();
                                lvl.Add("level", _gameManager.CurrentLevel - 1);

                                AnalyticEvents.ReportEvent("Interstitial", lvl);
                            }

                            isInterstitial = true;
                        };

                        p.interstitial.OnAdClosed += (sender, args) =>
                        {
                            Debug.Log($"AdMob OnAdClosed: {p.name}");
                            Request(p.name);
                        };

                        p.interstitial.OnPaidEvent += (sender, args) =>
                        {
                            Debug.Log($"AdMob OnPaid: {p.name}");

                            paidAdUnit = p.Id;
                            paidAdValue = args.AdValue;

                            isPaid = true;
                        };
                    }

                    p.interstitial.LoadAd(request);
                    break;
                case AdPlacement.Type.Rewarded:
                    if (p.rewarded == null)
                    {
                        if (isTest)
                            p.rewarded = new RewardedAd("ca-app-pub-3940256099942544/5224354917");
                        else
                            p.rewarded = new RewardedAd(p.Id);

                        p.rewarded.OnAdLoaded += (sender, args) => 
                        {
                            Debug.Log($"AdMob OnAdLoaded: {p.name}");
                        };

                        p.rewarded.OnAdOpening += (sender, args) => 
                        {
                            Debug.Log($"AdMob OnAdOpening: {p.name}");

                            LastRewardedAdWatch = DateTime.Now;

                            isEarnedReward = false;
                        };

                        p.rewarded.OnAdFailedToLoad += (sender, args) => 
                        {
                            Debug.LogWarning($"AdMob OnAdFailedToLoad: {p.name} Error: {args.LoadAdError.GetMessage()}");
                        };

                        p.rewarded.OnAdFailedToShow += (sender, args) => 
                        {
                            Debug.LogWarning($"AdMob OnAdFailedToShow: {p.name} Error: {args.AdError.GetMessage()}");
                            isRewardedFailed = true;
                        };

                        p.rewarded.OnUserEarnedReward += (sender, args) => 
                        {
                            Debug.Log($"AdMob OnUserEarnedReward: {p.name}");

                            isEarnedReward = true;
                        };

                        p.rewarded.OnAdClosed += (sender, args) => 
                        {
                            Debug.Log($"AdMob OnAdClosed: {p.name}");

                            StopCoroutine("CheckRewardCoroutine");
                            StartCoroutine("CheckRewardCoroutine");

                            Request(p.name);
                        };

                        p.rewarded.OnPaidEvent += (sender, args) =>
                        {
                            Debug.Log($"AdMob OnPaid: {p.name} {args.AdValue.Value} {args.AdValue.CurrencyCode}");

                            paidAdUnit = p.Id;
                            paidAdValue = args.AdValue;

                            isPaid = true;
                        };
                    }

                    p.rewarded.LoadAd(request);
                    break;
            }
        }
    }

    public void Show(string placement)
    {
        var p = GetPlacement(placement);

        if (p == null)
        {
            Debug.LogWarning($"AdMob Placement {placement} not found!");
        }
        else
        {
            if (!IsReady(placement))
                return;

            switch (p.type)
            {
                case AdPlacement.Type.Banner:
                    p.banner?.Show();
                    break;
                case AdPlacement.Type.Interstitial:
                    p.interstitial.Show();
                    break;
                case AdPlacement.Type.Rewarded:
                    isEarnedReward = false;

                    p.rewarded.Show();
                    break;
            }
        }
    }

    public void Show(string placement, Action<bool> success)
    {
        var p = GetPlacement(placement);

        if (p == null)
        {
            Debug.LogWarning($"AdMob Placement {placement} not found!");
        }
        else
        {
            if (interstitialCoroutine != null)
                StopCoroutine(interstitialCoroutine);

            interstitialCoroutine = StartCoroutine(ShowAdCoroutine(p, result =>
            {
                success.Invoke(result);
            }));
        }
    }

    [SerializeField] bool waitForShowAd;
    bool interstitialResult;
    Coroutine interstitialCoroutine;

    IEnumerator ShowAdCoroutine(AdPlacement placement, Action<bool> success)
    {
        interstitialResult = false;

        if (IsReady(placement.name))
        {
            waitForShowAd = true;

            Show(placement.name);

            yield return new WaitUntil(() => !waitForShowAd);

            success(interstitialResult);
        }
        else
        {
            Request(placement.name);

            success(interstitialResult);
        }
    }

    public void Hide()
    {
        foreach (var p in placements)
            p.banner?.Hide();
    }

    public void DestroyAd()
    {
        foreach (var p in placements)
        {
            if (p.banner != null) p.banner.Destroy();
            if (p.interstitial != null) p.interstitial.Destroy();
        }

        isBannerActive = false;
    }

    bool isInterstitial;
    bool isInterstitialFailed;
    bool isRewarded;
    bool isRewardedFailed;

    bool isPaid;
    string paidAdUnit;
    AdValue paidAdValue;

    bool isEarnedReward;

    WaitForSeconds checkRewardDelay = new WaitForSeconds(0.5f);

    private void Update()
    {
        if (isInterstitial) 
        {
            interstitialResult = true;
            isInterstitial = false;

            waitForShowAd = false;

            OnInterstitial?.Invoke();
        }

        if (isInterstitialFailed)
        {
            isInterstitialFailed = false;

            waitForShowAd = false;

            OnInterstitialFailed?.Invoke();
        }

        if (isRewarded)
        {
            isRewarded = false;
            OnRewarded?.Invoke();
        }

        if (isRewardedFailed)
        {
            isRewardedFailed = false;
            OnRewardedFailed?.Invoke();
        }

        if (isPaid)
        {
            isPaid = false;

            var revenue = new YandexAppMetricaRevenue((decimal)(paidAdValue.Value / 1000000f), paidAdValue.CurrencyCode);
            
            revenue.ProductID = paidAdUnit;
            revenue.Receipt = new YandexAppMetricaReceipt();

            AppMetrica.Instance.ReportRevenue(revenue);
            FirebaseManager.ReportRevenue(paidAdUnit, paidAdValue.Value / 1000000f, paidAdValue.CurrencyCode);
            
            Debug.Log($"AdMob Report revenue AdUnit: {paidAdUnit} Value: {paidAdValue.Value / 1000000f} Currency: {paidAdValue.CurrencyCode}");
        }
    }

    IEnumerator CheckRewardCoroutine()
    {
        yield return checkRewardDelay;

        if (isEarnedReward)
            isRewarded = true;
        else
            isRewardedFailed = true;
    }
    
    [Serializable]
    public class AdPlacement
    {
        public string name;
        public Type type;
        public int completeLevelsToShow;

        [SerializeField] string androidId;
        [SerializeField] string iosId;

        public string Id
        {
#if UNITY_ANDROID
            get => androidId;
#endif

#if UNITY_IOS
            get => iosId;
#endif
        }

        public BannerView banner;
        public InterstitialAd interstitial;
        public RewardedAd rewarded;

        public enum Type 
        {
            Banner,
            Interstitial,
            Rewarded
        }
    }
}
