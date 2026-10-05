using System;

namespace Assets.Scripts.Services.Statistics
{
    public class StatisticsTracker : MonoSingleton<StatisticsTracker>
    {
        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        public void LogEvent(String eventId)
        {
            // Firebase Analytics will be wired here when its Unity SDK is added.
        }

    }
}
