using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Core;
using AmalgamGames.Effects;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Abilities
{
    public class Slowmo : MonoBehaviour, IRestartable, IRespawnable
    {
        [Title("Events")]
        [FoldoutGroup("Events")][SerializeField] private DynamicEvent[] _activateEvents;
        [FoldoutGroup("Events")][SerializeField] private DynamicEvent[] _deactivateEvents;
        [FoldoutGroup("Events")][SerializeField] private DynamicEvent _reenableEvent;
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
        private bool _isSubscribedToEvents = false;

        private bool _isActive = false;
        private bool _canActivate = false;

        // DELEGATES
        private List<Delegate> _activateHandlers;
        private List<Delegate> _deactivateHandlers;

        // COROUTINES
        private Coroutine _drainRoutine = null;
        private Coroutine _lerpTimescaleRoutine = null;

        #region Lifecycle

        private void Start()
        {
            SubscribeToEvents();

            PHYSICS_TIMESTEP = Time.fixedDeltaTime;
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
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

        private void OnActivateEvent()
        {
            ActivateSlowmo();
        }

        private void OnActivateEventWithParam(object param)
        {
            if(param.GetType() == typeof(ChargingInfo))
            {
                ChargingInfo chargingInfo = (ChargingInfo)param;
                if(chargingInfo.chargingType == ChargingType.Slowmo)
                {
                    ActivateSlowmo();
                }
            }
            else
            {
                ActivateSlowmo();
            }
        }

        private void OnDeactivateEvent()
        {
            EndSlowmo();
        }

        private void OnDeactivateEventWithParam(object param)
        {
            EndSlowmo();
        }

        #endregion

        #region Slow-mo

        private void ActivateSlowmo()
        {
            if(!_isActive && _canActivate && HasJuice())
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

        private void ReenableSlowmo()
        {
            // Re-enable slowmo on first launch after respawn/restart
            if (!_canActivate)
            {
                _canActivate = true;
            }
        }

        private void ReenableSlowmoWithParam(object param)
        {
            ReenableSlowmo();
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

        private void SubscribeToEvents()
        {
            if (!_isSubscribedToEvents)
            {
                foreach (DynamicEvent activateEvent in _activateEvents)
                {
                    object rawObj = (object)activateEvent.EventSource;

                    Delegate activateHandler;

                    if (activateEvent.EventHasParam)
                    {
                        activateHandler = Tools.WireUpEvent(rawObj, activateEvent.EventName, this, nameof(OnActivateEventWithParam));
                    }
                    else
                    {
                        activateHandler = Tools.WireUpEvent(rawObj, activateEvent.EventName, this, nameof(OnActivateEvent));
                    }
                    activateEvent.EventHandler = activateHandler;
                }

                foreach (DynamicEvent deactivateEvent in _deactivateEvents)
                {
                    object rawObj = (object)deactivateEvent.EventSource;

                    Delegate deactivateHandler;

                    if (deactivateEvent.EventHasParam)
                    {
                        deactivateHandler = Tools.WireUpEvent(rawObj, deactivateEvent.EventName, this, nameof(OnDeactivateEventWithParam));
                    }
                    else
                    {
                        deactivateHandler = Tools.WireUpEvent(rawObj, deactivateEvent.EventName, this, nameof(OnDeactivateEvent));
                    }
                    deactivateEvent.EventHandler = deactivateHandler;
                }

                if(_reenableEvent.EventSource != null)
                {
                    object rawObj = (object)_reenableEvent.EventSource;

                    Delegate reenableHandler;

                    if(_reenableEvent.EventHasParam)
                    {
                        reenableHandler = Tools.WireUpEvent(rawObj, _reenableEvent.EventName, this, nameof(ReenableSlowmoWithParam));
                    }
                    else
                    {
                        reenableHandler = Tools.WireUpEvent(rawObj, _reenableEvent.EventName, this, nameof(ReenableSlowmo));
                    }

                    _reenableEvent.EventHandler = reenableHandler;

                }

                _isSubscribedToEvents = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_isSubscribedToEvents)
            {
                foreach (DynamicEvent activateEvent in _activateEvents)
                {
                    Tools.DisconnectEvent((object)activateEvent.EventSource, activateEvent.EventName, activateEvent.EventHandler);
                }

                foreach (DynamicEvent deactivateEvent in _deactivateEvents)
                {
                    Tools.DisconnectEvent((object)deactivateEvent.EventSource, deactivateEvent.EventName, deactivateEvent.EventHandler);
                }

                if(_reenableEvent.EventSource != null)
                {
                    Tools.DisconnectEvent((object)_reenableEvent.EventSource, _reenableEvent.EventName, _reenableEvent.EventHandler);
                }

                _isSubscribedToEvents = false;
            }
        }


        #endregion

    }
}