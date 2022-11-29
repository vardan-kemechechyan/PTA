using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI;

public class CompleteLevelScreen : UIScreenController
{
    [SerializeField] TextMeshProUGUI header;
    [SerializeField] TextMeshProUGUI reward;
    [SerializeField] Text noThanks;
    [SerializeField] Animation rewardAnimation;
    [SerializeField] Button claimRewardButton;
    [SerializeField] Button getRewardButton;

    bool skipInterstitial;
    bool playerTookTheReward;

    public override void OnShow()
    {
        base.OnShow();

        skipInterstitial = false;

        AdMob.Instance.skipInterstitial = false;

        header.text = $"LEVEL {PlayerPrefs.GetInt("level")} COMPLETE!";
        reward.text = $"+{GameManager.GetInstance().GetReward(false)}";

        getRewardButton.gameObject.SetActive(false);
        claimRewardButton.gameObject.SetActive(true);
        noThanks.gameObject.SetActive(true);

        playerTookTheReward = false;

        CancelInvoke("UpdateRewardButton");
        InvokeRepeating("UpdateRewardButton", 0, 1);

        //if (IronSourceManager.GetInstance().IsRewardedLoaded) 
        if(AdMob.Instance.IsReady("Rewarded_claim"))
            claimRewardButton.interactable = true;
        else claimRewardButton.interactable = false;

/*#if UNITY_EDITOR
        claimRewardButton.interactable = true;
#endif*/
    }

    private void OnDisable()
    {
        /*IronSourceManager.OnRewarded -= OnRewarded;
        IronSourceManager.OnRewardedFailed -= OnRewardedFailed;*/

        AdMob.OnRewarded -= Reward;
        AdMob.OnRewardedFailed -= RewardFailed;
    }

    private void UpdateRewardButton()
    {
        bool isAdReady = false;

        isAdReady = AdMob.Instance.IsReady("Rewarded_claim");

        claimRewardButton.interactable = isAdReady;
    }

    public void ClaimBonus()
    {
        /*IronSourceManager.OnRewarded -= Reward;
        IronSourceManager.OnRewardedFailed -= RewardFailed;
        IronSourceManager.OnRewarded += Reward;
        IronSourceManager.OnRewardedFailed += RewardFailed;*/

        AdMob.OnRewarded -= Reward;
        AdMob.OnRewardedFailed -= RewardFailed;
        AdMob.OnRewarded += Reward;
        AdMob.OnRewardedFailed += RewardFailed;

        AdMob.Instance.Show("Rewarded_claim");

        claimRewardButton.interactable = false;

/*#if UNITY_EDITOR
        Reward();
#endif*/
/*#if !UNITY_EDITOR
        IronSourceManager.OnRewarded += Reward;
        IronSourceManager.OnRewardedFailed += RewardFailed;
#endif

        IronSourceManager.GetInstance().ShowRewardedVideo("DefaultRewardedVideo");
        */
    }

    private void Reward()
    {
        /*IronSourceManager.OnRewarded -= Reward;
        IronSourceManager.OnRewardedFailed -= RewardFailed;*/

        AdMob.OnRewarded -= Reward;
        AdMob.OnRewardedFailed -= RewardFailed;

        playerTookTheReward = true;

        skipInterstitial = true;
        AdMob.Instance.skipInterstitial = true;

        var lvl = new Dictionary<string, object>();
        lvl.Add("level", GameManager.GetInstance().CurrentLevel);

        AnalyticEvents.ReportEvent("Rewarded_claim", lvl);

        int r = GameManager.GetInstance().GetReward(true);

        getRewardButton.gameObject.SetActive(true);
        claimRewardButton.gameObject.SetActive(false);
        noThanks.gameObject.SetActive(false);

        reward.text = $"+{r}";
        rewardAnimation.Play();
    }

    public void RewardFailed()
    {
        skipInterstitial = true;

        AdMob.OnRewarded -= Reward;
        AdMob.OnRewardedFailed -= RewardFailed;

        AdMob.Instance.skipInterstitial = true;

        /*IronSourceManager.OnRewarded -= Reward;
        IronSourceManager.OnRewardedFailed -= RewardFailed;

        claimRewardButton.interactable = true;*/
    }

    public void Claim()
    {
        /*GameManager.GetInstance().PlayerCash += GameManager.GetInstance().GetReward(true);
        GameManager.GetInstance().NextLevel();*/

        NextScreen();
    }

    public void Continue()
    {
        /*
            if (GameManager.GetInstance().CurrentLevel >= 3)
                IronSourceManager.GetInstance().ShowInterstitial();

        GameManager.GetInstance().PlayerCash += GameManager.GetInstance().GetReward(false);
        GameManager.GetInstance().NextLevel();
        */

        NextScreen();
    }

    void NextScreen()
    {
        CancelInvoke("UpdateRewardButton");

        if(skipInterstitial)
        {
            GameManager.GetInstance().PlayerCash += GameManager.GetInstance().GetReward(playerTookTheReward);
            GameManager.GetInstance().NextLevel();
        }
        else
        {       
            AdMob.Instance.Show("Interstitial", success =>
            {
                GameManager.GetInstance().PlayerCash += GameManager.GetInstance().GetReward(playerTookTheReward);
                GameManager.GetInstance().NextLevel();
            });
        }
    }
}
