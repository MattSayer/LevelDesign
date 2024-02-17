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
        [SerializeField] private float _juiceDrainRatePerSecond = 10;
        [SerializeField] private float _slowMoTimeScale = 0.5f;
        [SerializeField] private float _timeScaleTransitionTime = 0.5f;
        [Space]
        [Title("Components")]
        [SerializeField] private Transform _rocketControllerTransform;
        [SerializeField] private SharedFloatValue _juice;

        // CONSTANTS
        private float PHYSICS_TIMESTEP;

        // EVENTS
        public event Action OnSlowmoStart;
        public event Action OnSlowmoEnd;

        // STATE
        private bool _isSubscribedToCharging = false;

        private bool _isActive = false;
        private bool _canActivate = false;

        // COMPONENTS
        private IRocketController _rocketController;

        // COROUTINES
        private Coroutine _drainRoutine = null;
        private Coroutine _lerpTimescaleRoutine = null;

        #region Lifecycle

        private void Start()
        {
            _rocketController = Tools.GetFirstComponentInHierarchy<IRocketController>(_rocketControllerTransform);
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

        #region Respawning/restarting

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnCollision:
                case RespawnEvent.OnRespawnStart:
                    _canActivate = false;
                    CancelSlowmo();
                    break;
            }
        }

        public void DoRestart()
        {
            CancelSlowmo();
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

            // Enable slowmo on first launch after respawn/restart
            if (!_canActivate)
            {
                _canActivate = true;
            }
        }

        #endregion

        #region Slow-mo

        private void ActivateSlowmo()
        {
            if(_canActivate && HasJuice())
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
            if (_isActive)
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

        #region Juice

        private bool HasJuice()
        {
            return _juice.CanSubtract(Time.unscaledDeltaTime * _juiceDrainRatePerSecond);
        }

        #endregion

        #region Coroutines

        private IEnumerator drainSlowMo()
        {
            while(HasJuice())
            {
                _juice.SubtractValue(Time.unscaledDeltaTime * _juiceDrainRatePerSecond);
                yield return null;
            }
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