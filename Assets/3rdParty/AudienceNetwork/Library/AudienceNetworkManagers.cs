#if UNITY_IOS
using System.Runtime.InteropServices;

public class AudienceNetworkManagers
{
    [DllImport("__Internal")]
    private static extern void FBAdSettingsBridgeSetAdvertiserTrackingEnabled(bool advertiserTrackingEnabled);

    public static void SetAdvertiserTrackingEnabled(bool advertiserTrackingEnabled)
    {
#if !UNITY_EDITOR && UNITY_IOS
        FBAdSettingsBridgeSetAdvertiserTrackingEnabled(advertiserTrackingEnabled);
#endif
    }
}
#endif
