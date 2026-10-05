using System;
using Assets.Scripts.Extentions;
using FlurrySDK;

namespace Assets.Scripts.Services.Statistics
{
    public class StatisticsTracker : MonoSingleton<StatisticsTracker>
    {
        protected override void Awake()
        {
            base.Awake();
            InitializeFlurry();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        #region Flurry SDK
        private void InitializeFlurry()
        {
            new Flurry.Builder()
                  .WithCrashReporting(true)
                  .WithLogEnabled(true)
                  .WithLogLevel(Flurry.LogLevel.VERBOSE)
                  .WithMessaging(true)
#if UNITY_IPHONE || UNITY_IOS
                  .Build("J4YX7S9DNTGKPP25XZMJ");
#elif UNITY_ANDROID
                  .Build(null);
#else
                  .Build(null);
#endif
        }
        public void LogFlurryEvent(String eventId)
        {
            Flurry.LogEvent(eventId);
        }
        #endregion

        #region Facebook SDK
        /*private void InitializeFacebook()
        {
            if (!FB.IsInitialized)
            {
                // Initialize the Facebook SDK
                FB.Init(InitCallback, OnHideUnity);
            }
            else
            {
                // Already initialized, signal an app activation App Event
                FB.ActivateApp();
            }
        }

        private void InitCallback()
        {
            if (FB.IsInitialized)
            {
                // Signal an app activation App Event
                FB.ActivateApp();
                // Continue with Facebook SDK
                // ...
            }
            else
            {
                LoggerMethods.Log("Failed to Initialize the Facebook SDK");
            }
        }

        private void OnHideUnity(bool isGameShown)
        {
            if (!isGameShown)
            {
                // Pause the game - we will need to hide
                Time.timeScale = 0;
            }
            else
            {
                // Resume the game - we're getting focus again
                Time.timeScale = 1;
            }
        }*/
        #endregion
    }
}
