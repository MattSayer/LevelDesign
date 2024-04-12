using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Cinemachine;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class ScreenShaker : ManagedBehaviour, IPausable, IScreenShaker, IRespawnable
    {
        [Title("Components")]
        [SerializeField] private CinemachineVirtualCamera _playerCam;
        [Space]
        [Title("Dependency Provider")]
        [SerializeField] private DependencyRequest _getScreenShake;
        [Space]
        [Title("Dependency requests")]
        [SerializeField] private DependencyRequest _getPlayerPrefsCache;

        // STATE
        private bool _isSubscribedToDependencyRequests = false;
        private bool _isSubscribedToPlayerPrefsCache = false;
        private bool _isActive = true;

        // Components
        private CinemachineBasicMultiChannelPerlin _screenShake;

        // PlayerPrefs
        private PlayerPrefsCache _playerPrefsCache;

        // Player settings
        private float _screenShakeMultiplier = 1;

        // Shake buffers
        private float _amplitudeBuffer;
        private float _frequencyBuffer;

        private Dictionary<MonoBehaviour, ScreenShakeIntensity> _screenShakeRequests = new Dictionary<MonoBehaviour, ScreenShakeIntensity>();

        #region Lifecycle

        private void Awake()
        {
            SubscribeToDependencyRequests();
            _screenShake = _playerCam?.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

            _getPlayerPrefsCache.RequestDependency(ReceivePlayerPrefsCache);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToDependencyRequests();
            SubscribeToPlayerPrefsCache();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromDependencyRequests();
            UnsubscribeFromPlayerPrefsCache();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromDependencyRequests();
            UnsubscribeFromPlayerPrefsCache();
        }

        public override void ManagedUpdate(float deltaTime)
        {
            if (_isActive)
            {
                CalculateScreenShake();
                _screenShake.m_AmplitudeGain = _amplitudeBuffer * _screenShakeMultiplier;
                _screenShake.m_FrequencyGain = _frequencyBuffer * _screenShakeMultiplier;
                ClearBuffers();
            }
        }

        #endregion

        #region Pausing

        public void Pause()
        {
            _isActive = false;
            _screenShake.m_AmplitudeGain = 0;
            _screenShake.m_FrequencyGain = 0;
        }

        public void Resume()
        {
            _isActive = true;
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    StopAllCoroutines();
                    /*
                    _screenShakeRequests.Clear();
                    ClearBuffers();
                    */
                    break;
            }
        }

        #endregion

        #region PlayerPrefs

        private void OnScreenShakeAmountChanged(object value)
        {
            if(value.GetType() == typeof(float) || value.GetType() == typeof(int))
            {
                _screenShakeMultiplier = (float)value;
            }
        }

        #endregion

        #region Screen shake requests

        public void ContinuousScreenShake(MonoBehaviour instigator, ScreenShakeIntensity intensity)
        {
            _screenShakeRequests[instigator] = intensity;
        }

        public void StopContinuousScreenShake(MonoBehaviour instigator)
        {
            if(_screenShakeRequests.ContainsKey(instigator))
            {
                _screenShakeRequests.Remove(instigator);
            }
        }

        public void ScreenShakeBurst(ScreenShakeIntensity intensity, float duration, EasingFunction.Ease falloffIntensity = EasingFunction.Ease.Linear)
        {
            StartCoroutine(screenShakeBurst(intensity, duration, falloffIntensity));
        }

        private void CalculateScreenShake()
        {
            foreach(ScreenShakeIntensity intensity in _screenShakeRequests.Values)
            {
                _amplitudeBuffer = Mathf.Max(_amplitudeBuffer, intensity.Amplitude);
                _frequencyBuffer = Mathf.Max(_frequencyBuffer, intensity.Frequency);
            }
        }

        private void ClearBuffers()
        {
            _amplitudeBuffer = 0;
            _frequencyBuffer = 0;
        }

        private void AddScreenShake(ScreenShakeIntensity intensity)
        {
            _amplitudeBuffer = Mathf.Max(_amplitudeBuffer, intensity.Amplitude);
            _frequencyBuffer = Mathf.Max(_frequencyBuffer, intensity.Frequency);
        }

        #endregion

        #region Coroutines

        private IEnumerator screenShakeBurst(ScreenShakeIntensity intensity, float duration, EasingFunction.Ease falloffEasing)
        {
            float time = 0;
            EasingFunction.Function func = EasingFunction.GetEasingFunction(falloffEasing);
            while(time < duration)
            {
                float lerpVal = time / duration;
                float ampVal = func(intensity.Amplitude, 0, lerpVal);
                float freqVal = func(intensity.Frequency, 0, lerpVal);

                AddScreenShake(new ScreenShakeIntensity(ampVal, freqVal));

                time += Time.deltaTime;
                yield return null;
            }
        }

        #endregion

        #region Dependency Requests

        private void ProvideDependency(Action<object> callback)
        {
            callback?.Invoke((IScreenShaker)this);
        }

        private void SubscribeToDependencyRequests()
        {
            if (!_isSubscribedToDependencyRequests)
            {
                _getScreenShake.OnDependencyRequest += ProvideDependency;
                _isSubscribedToDependencyRequests = true;
            }
        }

        private void UnsubscribeFromDependencyRequests()
        {
            if (_isSubscribedToDependencyRequests)
            {
                _getScreenShake.OnDependencyRequest -= ProvideDependency;
                _isSubscribedToDependencyRequests = false;
            }
        }

        private void ReceivePlayerPrefsCache(object rawObj)
        {
            _playerPrefsCache = rawObj as PlayerPrefsCache;
            SubscribeToPlayerPrefsCache();
        }

        private void SubscribeToPlayerPrefsCache()
        {
            if (!_isSubscribedToPlayerPrefsCache && _playerPrefsCache != null)
            {
                _playerPrefsCache.SubscribeToValueChanged(this, Globals.SCREEN_SHAKE_AMOUNT_KEY, OnScreenShakeAmountChanged);
                _isSubscribedToPlayerPrefsCache = true;
            }
        }

        private void UnsubscribeFromPlayerPrefsCache()
        {
            if (_isSubscribedToPlayerPrefsCache && _playerPrefsCache != null)
            {
                _playerPrefsCache.UnsubscribeFromValueChanged(this, Globals.SCREEN_SHAKE_AMOUNT_KEY);
                _isSubscribedToPlayerPrefsCache = false;
            }
        }

        #endregion

    }

    public interface IScreenShaker
    {
        public void ScreenShakeBurst(ScreenShakeIntensity intensity, float duration, EasingFunction.Ease falloffIntensity = EasingFunction.Ease.Linear);
        public void ContinuousScreenShake(MonoBehaviour instigator, ScreenShakeIntensity intensity);
        public void StopContinuousScreenShake(MonoBehaviour instigator);
    }

    public struct ScreenShakeIntensity
    {
        public float Amplitude;
        public float Frequency;

        public ScreenShakeIntensity(float amplitude, float frequency)
        {
            this.Amplitude = amplitude;
            this.Frequency = frequency;
        }
    }
}