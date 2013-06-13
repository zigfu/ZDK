using UnityEngine;

namespace ZDK.Utility
{
    // Summary:
    //      The following class will make any class that inherits from it a singleton MonoBehaviour automatically
    //
    // Example Usage:
    //      public class CLASSNAME : Singleton<CLASSNAME> { }
    //
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (T)FindObjectOfType(typeof(T));
                    if (_instance == null)
                    {
                        GameObject go = new GameObject();
                        DontDestroyOnLoad(go);
                        go.name = typeof(T).ToString();
                        _instance = go.AddComponent<T>();
                    }
                    DontDestroyOnLoad(_instance);
                }
                return _instance;
            }
        }
    }
}
