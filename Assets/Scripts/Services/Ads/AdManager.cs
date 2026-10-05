using System;
using Assets.Scripts.Extentions;
using Assets.Scripts.Services.Ads.MediationProxy;
using IdleKnightHero.UI;

namespace Assets.Scripts.Services.Ads
{
    public class AdManager : MonoSingleton<AdManager>
    {
        public static event Action<String> AdNotFinished;
        public static event Action<String> AdFinished;

        public Boolean IsInterstitialAdReady => _mediationProxy.IsInterstitialAvailable();

        public Boolean IsRewardedVideoAdReady => _mediationProxy.IsRewardedVideoAvailable();

        private String _adTag;
        private Action _adFinishedCallback;
        private IAdMediationProxy _mediationProxy;
        protected override void Awake()
        {
            base.Awake();
            _mediationProxy = EditorAdMediationProxy.Create(gameObject.transform);
            _mediationProxy.Initialize();
            _mediationProxy.RewardedVideoFinished += OnRewardedVideoFinished;
            Invoke("ShowBanner", 3);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _mediationProxy.RewardedVideoFinished -= OnRewardedVideoFinished;
        }

        private void OnRewardedVideoFinished(Boolean isSuccess)
        {
            MusicManager.Instance.PlayMusicByClipNumber(0);//0 is default for camp
            if (!isSuccess)
            {
                _adFinishedCallback = null;
                AdNotFinished?.Invoke(_adTag);
                return;
            }
            switch (_adTag)
            {
                case "Cameplay":
                case "XPBoosterPackage":
                    break;
                default:
                    LoggerMethods.LogError("Unexpected reward tag: " + _adTag);
                    break;
            }
            LoggerMethods.Log("Ad finished :" + _adTag);
            GameplayCanvas.single.StartVictoryChest();
            AdFinished.SafeInvoke(_adTag);
            _adFinishedCallback.SafeInvoke();
            _adFinishedCallback = null;
        }

        public void ShowBanner()
        {
            _mediationProxy.ShowBanner();
        }

        public void ShowInterstitialAd()
        {
            _mediationProxy.ShowInterstitial();
        }

        public void ShowRewardedAd(String adTag, Action<Boolean> beforeAdShownCallback,Action adFinishedCallback = null)
        {
            if (IsRewardedVideoAdReady)
            {
                _adTag = adTag;
                beforeAdShownCallback.SafeInvoke(true);
                MusicManager.Instance.FadeToZero();
                _adFinishedCallback = adFinishedCallback;
                _mediationProxy.ShowRewardedVideo();
            }
            else
            {
                beforeAdShownCallback.SafeInvoke(false);
                LoggerMethods.LogError("Ad is unavailable when trying to ShowRewardedAd");
            }
        }

        public void ShowTestSuit()
        {
            _mediationProxy.ShowTestSuit();
        }

        public void IsVideoAdReady()
        {
            if (IsRewardedVideoAdReady)
            {
                LoggerMethods.LogError("Ad is available when trying to ShowRewardedAd");
            }
            else
            {
                LoggerMethods.LogError("Ad is unavailable when trying to ShowRewardedAd");
            }
        }

    }
}
