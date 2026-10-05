using Assets.Scripts.Extentions;
using MoreMountains.NiceVibrations;

namespace Assets.Scripts.Services.Vibrations
{
    public class VibrationsManager : MonoSingleton<VibrationsManager>
    {
        public void CallVibe(HapticTypes haptic)
        {
            MMVibrationManager.Haptic(haptic);
        }
    }
}