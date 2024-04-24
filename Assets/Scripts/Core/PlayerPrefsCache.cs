using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Core
{
    public class PlayerPrefsCache : MonoBehaviour
    {
        [Title("Keys")]
        [SerializeField] private PlayerPrefsKey[] _keys;
        [Space]
        [Title("Dependency Provider")]
        [SerializeField] private DependencyRequest _getPlayerPrefsCache;

        // Cache
        private Dictionary<string, object> _cache;
        private Dictionary<string, PrimitiveDataType> _keyTypes;

        // Subscribers
        private Dictionary<object, Dictionary<string, Delegate>> _subscriberDelegates;
        
        // Singleton
        private static PlayerPrefsCache _instance;

        // Events
        public event Action<string, object> OnValueChanged;

        // Dependency provider
        private bool _isSubscribedToDependencyRequests = false;

        #region Lifecycle

        private void Awake()
        {
            if(_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SubscribeToDependencyRequests();

            InitialiseCache();

        }

        private void OnEnable()
        {
            SubscribeToDependencyRequests();
        }

        private void OnDisable()
        {
            UnsubscribeFromDependencyRequests();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDependencyRequests();
        }

        #endregion

        #region Initialisation

        private void InitialiseCache()
        {
            _cache = new Dictionary<string, object>();
            _keyTypes = new Dictionary<string, PrimitiveDataType>();

            foreach(PlayerPrefsKey key in _keys)
            {
                PrimitiveDataType type = key.Type;
                _keyTypes[key.Key] = type;
                switch (type)
                {
                    case PrimitiveDataType.Int:
                        if (PlayerPrefs.HasKey(key.Key))
                        {
                            _cache[key.Key] = PlayerPrefs.GetInt(key.Key);
                        }
                        else
                        {
                            _cache[key.Key] = Int32.Parse(key.DefaultValue);
                        }
                        break;
                    case PrimitiveDataType.Float:
                        if (PlayerPrefs.HasKey(key.Key))
                        {
                            _cache[key.Key] = PlayerPrefs.GetFloat(key.Key);
                        }
                        else
                        {
                            _cache[key.Key] = float.Parse(key.DefaultValue);
                        }
                        break;
                    case PrimitiveDataType.String:
                        if (PlayerPrefs.HasKey(key.Key))
                        {
                            _cache[key.Key] = PlayerPrefs.GetString(key.Key);
                        }
                        else
                        {
                            _cache[key.Key] = key.DefaultValue;
                        }
                        
                        break;
                }
            }

            _subscriberDelegates = new Dictionary<object, Dictionary<string, Delegate>>();
        }


        #endregion

        #region PlayerPrefs

        public void SetValue(string key, object value)
        {
            if(_cache.ContainsKey(key))
            {
                PrimitiveDataType type = _keyTypes[key];
                switch(type)
                {
                    case PrimitiveDataType.Int:
                        if(value.GetType() == typeof(int))
                        {
                            PlayerPrefs.SetInt(key, (int)value);
                        }
                        break;
                    case PrimitiveDataType.Float:
                        if(value.GetType() == typeof(float))
                        {
                            PlayerPrefs.SetFloat(key, (float)value);
                        }
                        break;
                    case PrimitiveDataType.String:
                        if (value.GetType() == typeof(string))
                        {
                            PlayerPrefs.SetString(key, (string)value);
                        }
                        break;
                }
                _cache[key] = value;

                NotifyValueChangedSubscribers(key, value);
            }
        }

        public object GetValue(string key)
        {
            return _cache.ContainsKey(key) ? _cache[key] : null;
        }

        #endregion

        #region Subscriptions

        public void SubscribeToValueChanged(object subscriber, string key, Action<object> callback)
        {
            Action<string, object> dynamicEvent = (subkey, val) => { if (subkey == key) { callback(val); } };
            Delegate eventHandler = Tools.WireUpEvent(this, nameof(OnValueChanged), dynamicEvent.Target, dynamicEvent.Method);

            if(!_subscriberDelegates.ContainsKey(subscriber))
            {
                _subscriberDelegates[subscriber] = new Dictionary<string, Delegate>();
            }

            // Only subscribe if not already subscribed
            if (!_subscriberDelegates[subscriber].ContainsKey(key))
            {
                _subscriberDelegates[subscriber][key] = eventHandler;
            }

        }

        public void UnsubscribeFromValueChanged(object subscriber, string key)
        {
            if(_subscriberDelegates.ContainsKey(subscriber) && _subscriberDelegates[subscriber].ContainsKey(key))
            {
                Tools.DisconnectEvent(this, nameof(OnValueChanged), _subscriberDelegates[subscriber][key]);
            }
        }

        private void NotifyValueChangedSubscribers(string key, object value)
        {
            OnValueChanged?.Invoke(key, value);
        }

        #endregion

        #region Dependency Requests

        private void ProvideDependency(Action<object> callback)
        {
            callback?.Invoke(this);
        }

        private void SubscribeToDependencyRequests()
        {
            if (!_isSubscribedToDependencyRequests)
            {
                _getPlayerPrefsCache.OnDependencyRequest += ProvideDependency;
                _isSubscribedToDependencyRequests = true;
            }
        }

        private void UnsubscribeFromDependencyRequests()
        {
            if (_isSubscribedToDependencyRequests)
            {
                _getPlayerPrefsCache.OnDependencyRequest -= ProvideDependency;
                _isSubscribedToDependencyRequests = false;
            }
        }

        #endregion

        [Serializable]
        private class PlayerPrefsKey
        {
            public string Key;
            public PrimitiveDataType Type;
            public string DefaultValue;
        }

    }
}