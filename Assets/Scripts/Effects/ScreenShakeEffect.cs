using AmalgamGames.Core;
using AmalgamGames.Editor;
using AmalgamGames.Utils;
using AmalgamGames.Transformation;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class ScreenShakeEffect : ToggleEffect, IRespawnable
    {
        [Title("Value providers")]
        [RequireInterface(typeof(IValueProvider))]
        [FoldoutGroup("Amplitude")] [SerializeField] private UnityEngine.Object amplitudeValueProvider;
        [FoldoutGroup("Amplitude")] [SerializeField] private string _amplitudeValueKey;
        [FoldoutGroup("Amplitude")] [SerializeField] private ConditionalTransformationGroup[] _amplitudeTransformations;
        [RequireInterface(typeof(IValueProvider))]
        [FoldoutGroup("Frequency")][SerializeField] private UnityEngine.Object frequencyValueProvider;
        [FoldoutGroup("Frequency")][SerializeField] private string _frequencyValueKey;
        [FoldoutGroup("Frequency")][SerializeField] private ConditionalTransformationGroup[] _frequencyTransformations;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getScreenShaker;

        private IValueProvider _amplitudeValueProvider => amplitudeValueProvider as IValueProvider;
        private IValueProvider _frequencyValueProvider => frequencyValueProvider as IValueProvider;

        // State
        private bool _isSubscribedToValue = false;
        private float _currentAmplitude = 0;
        private float _currentFrequency = 0;

        // Components
        private IScreenShaker _screenShaker;

        #region Lifecycle

        protected override void Start()
        {
            base.Start();
            _getScreenShaker.RequestDependency(ReceiveScreenShaker);
            SubscribeToValue();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromValue();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromValue();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToValue();
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnCollision:
                case RespawnEvent.OnRespawnStart:
                    DeactivateEffect();
                    break;
            }
        }

        #endregion


        #region Dependencies

        private void ReceiveScreenShaker(object rawObj)
        {
            _screenShaker = rawObj as IScreenShaker;
        }

        #endregion

        #region Screen shake

        protected override void ActivateEffect()
        {
            _screenShaker.ContinuousScreenShake(this, new ScreenShakeIntensity(_currentAmplitude, _currentFrequency));
        }

        protected override void DeactivateEffect()
        {
            _screenShaker.StopContinuousScreenShake(this);
        }

        private void UpdateScreenShake()
        {
            _screenShaker.ContinuousScreenShake(this, new ScreenShakeIntensity(_currentAmplitude, _currentFrequency));
        }

        #endregion

        #region Dynamic values

        private void OnAmplitudeValueChanged(object rawValue)
        {
            if (rawValue.GetType() == typeof(float))
            {
                for(int i = 0; i < _amplitudeTransformations.Length; i++)
                {
                    rawValue = _amplitudeTransformations[i].TransformObject(rawValue);
                }
            }

            _currentAmplitude = (float)rawValue;

            UpdateScreenShake();
            
        }

        private void OnFrequencyValueChanged(object rawValue)
        {
            if (rawValue.GetType() == typeof(float))
            {
                for (int i = 0; i < _frequencyTransformations.Length; i++)
                {
                    rawValue = _frequencyTransformations[i].TransformObject(rawValue);
                }
            }

            _currentFrequency = (float)rawValue;

            UpdateScreenShake();

        }

        #endregion

        #region Subscriptions

        private void SubscribeToValue()
        {
            if(!_isSubscribedToValue)
            {
                if (_amplitudeValueProvider != null)
                {
                    _amplitudeValueProvider.SubscribeToValue(_amplitudeValueKey, OnAmplitudeValueChanged);
                }
                if(_frequencyValueProvider != null)
                {
                    _frequencyValueProvider.SubscribeToValue(_frequencyValueKey, OnFrequencyValueChanged);
                }
                _isSubscribedToValue = true;
            }

        }

        private void UnsubscribeFromValue()
        {
            if (_isSubscribedToValue)
            {
                if (_amplitudeValueProvider != null)
                {
                    _amplitudeValueProvider.UnsubscribeFromValue(_amplitudeValueKey, OnAmplitudeValueChanged);
                }
                if (_frequencyValueProvider != null)
                {
                    _frequencyValueProvider.UnsubscribeFromValue(_frequencyValueKey, OnFrequencyValueChanged);
                }
                _isSubscribedToValue = false;
            }
        }

        #endregion
    }
}