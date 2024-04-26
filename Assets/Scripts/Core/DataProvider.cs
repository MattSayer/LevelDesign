using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Core
{
    public class DataProvider : MonoBehaviour, IValueProvider, IDataProvider
    {
        [Title("Data events")]
        [SerializeField] private DynamicEvent[] _dataEvents;
        
        // State
        private bool _isSubscribedToEvents = false;
        
        private Dictionary<string, EventContainer> _events = new Dictionary<string, EventContainer>();
        
        #region Data Provider
        
        public void ProvideData(object[] data)
        {
            // Flatten provided objects into dictionary
            Dictionary<string, object> flattenedData = Tools.GetPropertyDictionary(data);
            
            // Broadcast value changed event for each key
            foreach(string key in flattenedData.Keys)
            {
                if(_events.ContainsKey(key))
                {
                    _events[key].BroadcastValueChanged(flattenedData[key]);
                }
            }
        }
        
        private void ProvideData(DynamicEvent evt, object param)
        {
            bool conditionalCheck = Tools.ApplyConditionals(param, evt.Conditionals);
            if(conditionalCheck)
            {
                ProvideData(new object[] { param });
            }
        }
        
        #endregion
        
        #region Value provider
        
        public void SubscribeToValue(string valueName, Action<object> callback)
        {
            if(!_events.ContainsKey(valueName))
            {
                _events[valueName] = new EventContainer();
            }
            
            _events[valueName].OnValueChanged += callback;
        }

        public void UnsubscribeFromValue(string valueName, Action<object> callback)
        {
            if(_events.ContainsKey(valueName))
            {
                _events[valueName].OnValueChanged -= callback;
            }
        }
        
        #endregion
        
        #region Subscriptions

        private void SubscribeToEvents()
        {
            if (!_isSubscribedToEvents)
            {
                // Pass null event for no-parameter hookup, since we need parameter data in order to provide it 
                Tools.SubscribeToDynamicEvents(_dataEvents, null, (dynEvent, param) => { ProvideData(dynEvent, param); });

                _isSubscribedToEvents = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_isSubscribedToEvents)
            {
                Tools.UnsubscribeFromDynamicEvents(_dataEvents);

                _isSubscribedToEvents = false;
            }
        }

        #endregion
        
        private class EventContainer
        {
            public event Action<object> OnValueChanged;
            public void BroadcastValueChanged(object value)
            {
                OnValueChanged?.Invoke(value);
            }
        }
    }
    
    public interface IDataProvider
    {
        public void ProvideData(object[] data);
    }
}