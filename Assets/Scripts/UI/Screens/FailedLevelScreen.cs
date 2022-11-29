using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UI;

public class FailedLevelScreen : UIScreenController
{
    [SerializeField] TextMeshProUGUI stage;
    [SerializeField] Button noThanksButton;
    [SerializeField] Button rewardButton;
    [SerializeField] Button restartButton;

    int stageIndex;

    bool skipInterstitial;

    public override void OnShow()
    {
        base.OnShow();

        skipInterstitial = false;

        AdMob.Instance.skipInterstitial = false;

        stageIndex = GameManager.GetInstance().GetStageIndex(GameManager.GetInstance().stage);

        stage.text = (stageIndex + 1).ToString();

        noThanksButton.gameObject.SetActive(false);
        rewardButton.gameObject.SetActive(false);
        restartButton.gameObject.SetActive(false);

        CancelInvoke("UpdateRewardButton");
        InvokeRepeating("UpdateRewardButton", 0, 1);

        if (stageIndex <= 0)
        {
            restartButton.gameObject.SetActive(true);
        }
        else 
        {
            noThanksButton.gameObject.SetActive(true);
            rewardButton.gameObject.SetActive(true);
        }

        //if (IronSourceManager.GetInstance().IsRewardedLoaded)
        if(AdMob.Instance.IsReady("Rewarded_rewind"))
            rewardButton.interactable = true;
        else rewardButton.interactable = false;

/*#if UNITY_EDITOR
        rewardButton.interactable = true;
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

        isAdReady = AdMob.Instance.IsReady("Rewarded_rewind");

        rewardButton.interactable = isAdReady;
    }

    public void Retry()
    {
        /*IronSourceManager.OnRewarded -= Reward;
        IronSourceManager.OnRewardedFailed -= RewardFailed;
        IronSourceManager.OnRewarded += Reward;
        IronSourceManager.OnRewardedFailed += RewardFailed;*/

        AdMob.OnRewarded -= Reward;
        AdMob.OnRewardedFailed -= RewardFailed;
        AdMob.OnRewarded += Reward;
        AdMob.OnRewardedFailed += RewardFailed;

        AdMob.Instance.Show("Rewarded_rewind");

        rewardButton.interactable = false;

/*#if UNITY_EDITOR
        Reward();
#endif*/
/*#if !UNITY_EDITOR
        IronSourceManager.OnRewarded += Reward;
        IronSourceManager.OnRewardedFailed += RewardFailed;
#endif

        IronSourceManager.GetInstance().ShowRewardedVideo("DefaultRewardedVideo");*/
    }

    private void Reward()
    {
        /*IronSourceManager.OnRewarded -= Reward;
        IronSourceManager.OnRewardedFailed -= RewardFailed;*/

        AdMob.OnRewarded -= Reward;
        AdMob.OnRewardedFailed -= RewardFailed;

        skipInterstitial = true;
        AdMob.Instance.skipInterstitial = true;

        var lvl = new Dictionary<string, object>();
        lvl.Add("level", GameManager.GetInstance().CurrentLevel);

        AnalyticEvents.ReportEvent("Rewarded_rewind", lvl);
        rewardButton.interactable = true;

        GameManager.GetInstance().RetryStage();
    }

    public void RewardFailed()
    {
        /*IronSourceManager.OnRewarded -= Reward;
        IronSourceManager.OnRewardedFailed -= RewardFailed;*/

        skipInterstitial = true;

        AdMob.OnRewarded -= Reward;
        AdMob.OnRewardedFailed -= RewardFailed;

        AdMob.Instance.skipInterstitial = true;

        rewardButton.interactable = true;
    }

    public void Continue()
    {
        CancelInvoke("UpdateRewardButton");

        if (stageIndex > 0)
        {
            var lvl = new Dictionary<string, object>();
            lvl.Add("level", GameManager.GetInstance().CurrentLevel);

            AnalyticEvents.ReportEvent("lvl_cancel", lvl);
        }

        GameManager.GetInstance().RestartStage();
    }
}
