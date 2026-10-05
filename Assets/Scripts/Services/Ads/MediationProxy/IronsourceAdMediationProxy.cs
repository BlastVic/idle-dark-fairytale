using System;
using System.Collections;
using Assets.Scripts.Extentions;
using UnityEngine;

namespace Assets.Scripts.Services.Ads.MediationProxy
{
    public class IronsourceAdMediationProxy : MonoBehaviour, IAdMediationProxy
    {
        private const Single AdReloadPeriod = 3f;
        private const Int32 MaxFailedDisplaysCount = 10;
        private const Single AdFinishWaitTime = 1f;

#if UNITY_ANDROID
#if ENABLE_ADS_TEST_SUIT
        private const String AppKey = "";
#else
        private const String AppKey = "";
#endif
#else
#if ENABLE_ADS_TEST_SUIT
        private const String AppKey = "";
#else
        private const String AppKey = "cd6264cd";
#endif
#endif

        public event Action<bool> RewardedVideoFinished;
        private Boolean _isRewardedVideoAvailable;
        private Boolean _isRewardedVideoIncentivized;
        private Int32 _failedDisplaysCount;

        public static IAdMediationProxy Create(Transform parent)
        {
            var gameObject = new GameObject("IronsourceAdMediationProxy");
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent<IronsourceAdMediationProxy>();
        }

        #region IAdMediationProxy
        public void Initialize()
        {
#if UNITY_IOS
            AudienceNetworkManagers.SetAdvertiserTrackingEnabled(true);
#endif
            IronSource.Agent.setMetaData("Facebook_IS_CacheFlag", "IMAGE");
            IronSource.Agent.init(AppKey);
            IronSource.Agent.validateIntegration();
            //IronSource.Agent.setAdaptersDebug(true);
            LoadInterstitialAd(true);
            LoadRewardedVideoAd();
        }

        public void ShowBanner()
        {
            IronSource.Agent.loadBanner(IronSourceBannerSize.SMART, IronSourceBannerPosition.BOTTOM);
        }

        private IEnumerator _interstitialAdReloadCotoutinePointer;
        private void LoadInterstitialAd()
        {
            LoadInterstitialAd(false);
        }
        private void LoadInterstitialAd(Boolean isFirstLoad = false)
        {
            if (!isFirstLoad)
            {
                IronSourceEvents.onInterstitialAdReadyEvent -= OnInterstitialAdReady;
                IronSourceEvents.onInterstitialAdLoadFailedEvent -= OnInterstitialAdFailedToLoad;
                IronSourceEvents.onInterstitialAdClosedEvent -= OnInterstitialAdClosed;
            }
            IronSource.Agent.loadInterstitial();
            IronSourceEvents.onInterstitialAdReadyEvent += OnInterstitialAdReady;
            IronSourceEvents.onInterstitialAdLoadFailedEvent += OnInterstitialAdFailedToLoad;
            IronSourceEvents.onInterstitialAdClosedEvent += OnInterstitialAdClosed;
        }

        private void OnInterstitialAdReady()
        {
            LoggerMethods.Log("Ready to insterstitial ad");
        }

        private void OnInterstitialAdFailedToLoad(IronSourceError error)
        {
            LoggerMethods.Log("Failed to load interstitial ad: " + error.getErrorCode() + " , " + error.getCode() + " , " + error.getDescription());
            ThreadRouter.RouteCallbackToUnityThread(() => CoroutinesHolder.AfterRealSeconds(ref _interstitialAdReloadCotoutinePointer, AdReloadPeriod, LoadInterstitialAd));
        }

        private void OnInterstitialAdClosed()
        {
            LoadInterstitialAd();
        }

        public Boolean IsInterstitialAvailable()
        {
            return IronSource.Agent.isInterstitialReady();
        }

        public void ShowInterstitial()
        {
            IronSource.Agent.showInterstitial();
        }

        private IEnumerator _rewardedAdReloadCoroutinePointer;
        private void LoadRewardedVideoAd()
        {
            IronSourceEvents.onRewardedVideoAdOpenedEvent += OnRewardedVideoAdOpened;
            IronSourceEvents.onRewardedVideoAdShowFailedEvent += OnRewardedVideoAdFailedToShow;
            IronSourceEvents.onRewardedVideoAdRewardedEvent += OnRewardedVideoAdUserEarnedReward;
            IronSourceEvents.onRewardedVideoAdClosedEvent += OnRewardedVideoAdClosed;
            IronSourceEvents.onRewardedVideoAvailabilityChangedEvent += OnRewardedVideoAdAvailabilityChanged;
            IronSourceEvents.onRewardedVideoAdStartedEvent += OnRewardedVideoAdStarted;
            IronSourceEvents.onRewardedVideoAdEndedEvent += OnRewardedVideoAdEnded;
        }

        private void OnRewardedVideoAdStarted()
        {
            LoggerMethods.Log("Stared to reward video ad");
        }

        private void OnRewardedVideoAdEnded()
        {
            LoggerMethods.Log("Ended to reward video ad");
        }

        private void OnRewardedVideoAdOpened()
        {
            LoggerMethods.Log("Opened to reward video ad");
        }

        private void OnRewardedVideoAdAvailabilityChanged(bool available)
        {
            LoggerMethods.Log("Availability Changed to reward video ad: " + available);
        }

        private void OnRewardedVideoAdFailedToShow(IronSourceError error)
        {
            LoggerMethods.LogError("Failed to show rewarded video ad: " + error.getErrorCode() + " , " + error.getCode() + " , " + error.getDescription());
            OnRewardedVideoAdClosed();
        }

        private void OnRewardedVideoAdUserEarnedReward(IronSourcePlacement placement)
        {
            Debug.Log("User earned to reward video ad:" + placement.getPlacementName());

            _isRewardedVideoIncentivized = true;
        }

        private void OnRewardedVideoAdClosed()
        {
            Debug.Log("Closed to reward video ad");
            _failedDisplaysCount = 0;
            if (_isRewardedVideoIncentivized)
            {
                ThreadRouter.RouteCallbackToUnityThread(() => RewardedVideoFinished.SafeInvoke(true));
            }
            else
            {
                ThreadRouter.RouteCallbackToUnityThread(() =>
                {
                    CoroutinesHolder.Instance.AfterSeconds(AdFinishWaitTime, () =>
                    {
                        RewardedVideoFinished.SafeInvoke(_isRewardedVideoIncentivized);
                    });
                });
            }
        }

        public Boolean IsRewardedVideoAvailable()
        {
            return IronSource.Agent.isRewardedVideoAvailable();
        }

        public void ShowRewardedVideo()
        {
            _isRewardedVideoIncentivized = false;
            IronSource.Agent.showRewardedVideo();
        }

        public void ShowTestSuit()
        {
#if ENABLE_ADS_TEST_SUIT
            IronSource.Agent.validateIntegration();
#endif
        }
        #endregion
    }
}