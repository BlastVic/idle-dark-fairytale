using System;

namespace Assets.Scripts.Services.Ads.MediationProxy
{
    public interface IAdMediationProxy
    {
        event Action<Boolean> RewardedVideoFinished;

        void Initialize();
        Boolean IsInterstitialAvailable();
        Boolean IsRewardedVideoAvailable();
        void ShowBanner();
        void ShowInterstitial();
        void ShowRewardedVideo();
        void ShowTestSuit();
    }
}