using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Core;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Abilities
{
    public class Slowmo : MonoBehaviour, IRestartable, IRespawnable
    {
        [Title("Settings")]
        [SerializeField] private float _defaultMaxJuice = 100;
        [SerializeField] private float _juiceDrainRatePerSecond = 10;
        [SerializeField] private float _slowMoTimeScale = 0.5f;
        [SerializeField] private float _timeScaleTransitionTime = 0.5f;

        // CONSTANTS
        private float PHYSICS_TIMESTEP;

        // EVENTS
        public event Action<float> OnJuiceLevelChanged;
        public event Action OnSlowmoStart;
        public event Action OnSlowmoEnd;


        // STATE
        private bool _isSubscribedToCharging = false;
        private float _maxJuice;
        private float _currentJuice;

        private bool _isInitialised = false;
        private bool _isActive = false;

        // COMPONENTS
        private IRocketController _rocketController;

        // COROUTINES
        private Coroutine _drainRoutine = null;
        private Coroutine _lerpTimescaleRoutine = null;

        #region Lifecycle

        private void Start()
        {
            _rocketController = Tools.GetFirstComponentInHierarchy<IRocketController>(transform.parent);
            SubscribeToCharging();

            PHYSICS_TIMESTEP = Time.fixedDeltaTime;
        }

        private void OnEnable()
        {
            SubscribeToCharging();
        }

        private void OnDisable()
        {
            UnsubscribeFromCharging();
        }

        private void OnDestroy()
        {
            UnsubscribeFromCharging();
        }

        #endregion

        #region Initialisation

        public void Initialise(float maxJuice)
        {
            _maxJuice = maxJuice;
            _currentJuice = _maxJuice;
            _isInitialised = true;
        }

        #endregion

        #region Respawning/restarting

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnCollision:
                    CancelSlowmo();
                    break;
                case RespawnEvent.OnRespawnStart:
                    CancelSlowmo();
                    break;
            }
        }

        public void DoRestart()
        {
            _currentJuice = _maxJuice;
        }

        #endregion

        #region Charging/Launching

        private void OnChargingStart(ChargingType chargingType)
        {
            if(chargingType == ChargingType.Slowmo)
            {
                ActivateSlowmo();
            }
        }

        private void OnLaunch(LaunchInfo launchInfo)
        {
            EndSlowmo();
        }

        #endregion

        #region Slow-mo

        private void ActivateSlowmo()
        {
            // If for some reason the level manager hasn't initialised the slow-mo component, set to defaults
            if(!_isInitialised)
            {
                _maxJuice = _defaultMaxJuice;
                _currentJuice = _maxJuice;
                _isInitialised = true;
            }

            if(_currentJuice > 0)
            {
                _isActive = true;

                StopAllCoroutines();

                _drainRoutine = StartCoroutine(drainSlowMo());
                OnSlowmoStart?.Invoke();

                float timeScaleDiff = (Time.timeScale - _slowMoTimeScale) / (1 - _slowMoTimeScale);

                _lerpTimescaleRoutine = StartCoroutine(Tools.lerpFloatOverTimeUnscaled(Time.timeScale, _slowMoTimeScale, _timeScaleTransitionTime * timeScaleDiff,
                        (value) => 
                        {
                            Time.timeScale = value;
                            Time.fixedDeltaTime = PHYSICS_TIMESTEP * value;
                        },
                        () =>
                        {
                            Time.timeScale = _slowMoTimeScale;
                            Time.fixedDeltaTime = PHYSICS_TIMESTEP * _slowMoTimeScale;
                            _lerpTimescaleRoutine = null;
                        }
                    ));
            }
        }

        private void EndSlowmo()
        {
            StopAllCoroutines();

            float timeScaleDiff = (1 - Time.timeScale) / (1 - _slowMoTimeScale);

            _lerpTimescaleRoutine = StartCoroutine(Tools.lerpFloatOverTimeUnscaled(Time.timeScale, 1, _timeScaleTransitionTime * timeScaleDiff,
                    (value) =>
                    {
                        Time.timeScale = value;
                        Time.fixedDeltaTime = value * PHYSICS_TIMESTEP;
                    },
                    () =>
                    {
                        Time.timeScale = 1;
                        Time.fixedDeltaTime = PHYSICS_TIMESTEP;
                        _lerpTimescaleRoutine = null;
                    }
                ));


            _isActive = false;
            OnSlowmoEnd?.Invoke();
            
        }

        private void CancelSlowmo()
        {
            if(_isActive)
            {
                StopAllCoroutines();

                Time.fixedDeltaTime = PHYSICS_TIMESTEP;
                Time.timeScale = 1;

                OnSlowmoEnd?.Invoke();
                _isActive = false;
            }
        }

        #endregion

        #region Coroutines

        private IEnumerator drainSlowMo()
        {
            while(_currentJuice > 0)
            {
                _currentJuice -= Time.unscaledDeltaTime * _juiceDrainRatePerSecond;
                // Pass normalized juice level to subscribers
                OnJuiceLevelChanged?.Invoke(_currentJuice / _maxJuice);
                yield return null;
            }
            _currentJuice = 0;
            OnJuiceLevelChanged?.Invoke(0);
            _drainRoutine = null;
            EndSlowmo();
        }

        #endregion

        #region Subscriptions

        private void SubscribeToCharging()
        {
            if (!_isSubscribedToCharging && _rocketController != null)
            {
                _rocketController.OnChargingStart += OnChargingStart;
                _rocketController.OnLaunch += OnLaunch;
                _isSubscribedToCharging = true;
            }
        }

        private void UnsubscribeFromCharging()
        {
            if (_isSubscribedToCharging && _rocketController != null)
            {
                _rocketController.OnChargingStart -= OnChargingStart;
                _rocketController.OnLaunch -= OnLaunch;
                _isSubscribedToCharging = false;
            }
        }


        #endregion

    }
}