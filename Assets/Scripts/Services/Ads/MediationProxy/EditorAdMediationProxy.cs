using System;
using UnityEngine;

namespace Assets.Scripts.Services.Ads.MediationProxy
{
    public class EditorAdMediationProxy : MonoBehaviour, IAdMediationProxy
    {
        public event Action<bool> RewardedVideoFinished;

        public static IAdMediationProxy Create(Transform parent)
        {
            var gameObject = new GameObject("EditorAdMediationProxy");
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent<EditorAdMediationProxy>();
        }

        #region IAdMediationProxy
        public void Initialize()
        {

        }

        public void ShowBanner()
        {

        }

        public Boolean IsInterstitialAvailable()
        {
            return false;
        }

        public void ShowInterstitial()
        {

        }

        public Boolean IsRewardedVideoAvailable()
        {
            return false;
        }

        public void ShowRewardedVideo()
        {

        }

        public void ShowTestSuit()
        {

        }
        #endregion
    }
}
