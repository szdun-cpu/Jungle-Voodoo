using UnityEngine;

namespace JungleVoodoo.Utilities
{
    /// <summary>
    /// Generic MonoBehaviour singleton base. Subclass it to get a DontDestroyOnLoad
    /// singleton with automatic Instance management.
    ///
    /// Usage:
    ///   public class AudioManager : Singleton&lt;AudioManager&gt; { ... }
    ///
    /// Prefer ServiceLocator for systems that need to be testable or swappable.
    /// Use Singleton only for MonoBehaviours that truly need global, persistent access
    /// and have no alternative implementation.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new();

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = FindFirstObjectByType<T>();

                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
