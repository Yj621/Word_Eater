using System;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using UnityEngine;

public class AdsManager : MonoBehaviour
{
    public static AdsManager I { get; private set; }

#if UNITY_ANDROID
    [SerializeField] private string rewardedAdRevivalId = "ca-app-pub-1881501262849586/3221896109"; // 테스트용
#elif UNITY_IOS
    [SerializeField] private string rewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313"; // 테스트용
#else
    [SerializeField] private string rewardedAdUnitId = "unused";
#endif

    private RewardedAd _rewardedAd;
    private bool _initializing;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeSdkIfNeeded(() => PreloadRewarded());
    }

    public void InitializeSdkIfNeeded(Action onDone = null)
    {
        if (_initializing) return;
        _initializing = true;

        MobileAds.Initialize((InitializationStatus status) =>
        {
            _initializing = false;
            onDone?.Invoke();
        });
    }

    // 미리 로드
    public void PreloadRewarded()
    {
        if (string.IsNullOrEmpty(rewardedAdRevivalId)) return;

        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        var request = new AdRequest();
        RewardedAd.Load(rewardedAdRevivalId, request, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogWarning($"[Ads] Rewarded load failed: {error.GetMessage()}");
                return;
            }

            _rewardedAd = ad;
            HookRewardedEvents(_rewardedAd);
            Debug.Log("[Ads] Rewarded loaded.");
        });
    }

    private void HookRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[Ads] Rewarded opened.");
        };
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[Ads] Rewarded closed.");
            // 닫히면 다음 광고 미리 로드
            PreloadRewarded();
        };
        ad.OnAdFullScreenContentFailed += (AdError err) =>
        {
            Debug.LogWarning($"[Ads] Rewarded open failed: {err.GetMessage()}");
            PreloadRewarded();
        };
    }

    /// <summary>
    /// 공용: 리워드 광고 표시 (보상 콜백/미준비 콜백)
    /// </summary>
    public void ShowRewarded(Action onRewardEarned, Action onUnavailable = null)
    {
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show((Reward reward) =>
            {
                onRewardEarned?.Invoke();
            });
        }
        else
        {
            Debug.LogWarning("[Ads] Rewarded not ready.");
            onUnavailable?.Invoke();
            PreloadRewarded();
        }
    }

    /// <summary>
    /// 부활 전용 진입점
    /// </summary>
    public void ShowReviveAd(Action onRevive, Action onFail = null)
    {
        ShowRewarded(
            onRewardEarned: onRevive,
            onUnavailable: onFail
        );
    }
}
