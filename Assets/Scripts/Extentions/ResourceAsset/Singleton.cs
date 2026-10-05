namespace Assets.Scripts.Extentions.ResourceAsset
{
    public class Singleton<T> where T : class, new()
    {
        private static T _instance;

        public static T Instance
        {
            get { return _instance ?? (_instance = new T()); }
        }
    }
}
