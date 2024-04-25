using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Timing
{
    public class Timer : MonoBehaviour, IValueProvider, ITimer
    {
        [Title("Settings")]
        [SerializeField] private bool _startOnActivate = false;
        
        // State
        private bool _isActive = false;
        
        private float _time = 0;
        
        // Coroutines
        private Coroutine _timerRoutine = null;
        
        // Events
        private event Action<object> OnValueChanged;

        #region Lifecycle
        
        private void OnEnable()
        {
            if(_startOnActivate)
            {
                StartTimer();
            }
        }
        
        private void OnDisable()
        {
            StopTimer();
        }
        
        #endregion

        #region Timer

        public void StartTimer()
        {
            if(!_isActive && _timerRoutine == null)
            {
                _isActive = true;
                _timerRoutine = StartCoroutine(timer());
            }
        }
        
        public void StopTimer()
        {
            if(_isActive)
            {
                if(_timerRoutine != null)
                {
                    StopCoroutine(_timerRoutine);
                    _timerRoutine = null;
                }
                _isActive = false;    
            }
        }
        
        public void ResetTimer()
        {
            _time = 0;
        }

        #endregion

        #region Coroutines
        
        private IEnumerator timer()
        {
            while(_isActive)
            {
                _time += Time.deltaTime;
                OnValueChanged?.Invoke(_time);
                yield return null;
            }
        }
        
        #endregion

        #region Value provider

        public void SubscribeToValue(string valueName, Action<object> callback)
        {
            switch(valueName)
            {
                case Globals.TIMER_CHANGED_KEY:
                    OnValueChanged += callback;
                    break;
            }
        }

        public void UnsubscribeFromValue(string valueName, Action<object> callback)
        {
            switch(valueName)
            {
                case Globals.TIMER_CHANGED_KEY:
                    OnValueChanged -= callback;
                    break;
            }
        }
        
        #endregion
    }
    
    public interface ITimer
    {
        public void StartTimer();
        public void StopTimer();
        public void ResetTimer();
    }
}