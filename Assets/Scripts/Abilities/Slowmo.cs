using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Conditionals;
using AmalgamGames.Core;
using AmalgamGames.Effects;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Abilities
{
    public class Slowmo : MonoBehaviour, IRestartable, IRespawnable, IPausable
    {
        [Title("Events")]
        [FoldoutGroup("Events")][SerializeField] private DynamicEvent[] _cancelEvents;
        [FoldoutGroup("Events")][SerializeField] private DynamicEvent[] _reenableEvents;
        [Title("Settings")]
        [SerializeField] private float _juiceDrainRatePerSecond = 10;
        [SerializeField] private float _slowMoTimeScale = 0.5f;
        [SerializeField] private float _timeScaleTransitionTime = 0.5f;
        [Space]
        [Title("Components")]
        [SerializeField] private Transform _rocketObject;
        [SerializeField] private SharedFloatValue _juice;

        // CONSTANTS
        private float PHYSICS_TIMESTEP;

        // EVENTS
        public event Action OnSlowmoStart;
        public event Action OnSlowmoEnd;

        // STATE
        private bool _isSubscribedToEvents = false;
        private bool _isSubscribedToInput = false;

        private bool _isActive = false;
        private bool _canActivate = false;
        private bool _isPaused = false;
        
        private bool _isLocked = false;

        private bool _cachedIsActive = false;

        // COROUTINES
        private Coroutine _drainRoutine = null;
        private Coroutine _lerpTimescaleRoutine = null;

        // COMPONENTS
        private IInputProcessor _inputProcessor;

        #region Lifecycle

        private void Start()
        {
            _inputProcessor = Tools.GetFirstComponentInHierarchy<IInputProcessor>(_rocketObject);
            
            SubscribeToInput();
            SubscribeToEvents();

            PHYSICS_TIMESTEP = Time.fixedDeltaTime;
        }

        private void OnEnable()
        {
            SubscribeToInput();
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInput();
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

        #region Pausing

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;

        }

        #endregion

        #region Charging/Launching


        private void OnLaunch(LaunchInfo launchInfo)
        {
            EndSlowmo();

            // Enable slowmo on first launch after respawn/restart
            if (!_canActivate)
            {
                _canActivate = true;
            }
        }

        private void OnSlowmoInputChange(float inputVal)
        {
            if (_isPaused)
            {
                _cachedIsActive = inputVal > 0;
            }
            else
            {
                if (inputVal > 0 && !_isActive)
                {
                    ActivateSlowmo();
                }
                else if (inputVal == 0 && _isActive)
                {
                    EndSlowmo();
                }
            }

            // Remove lock when trigger is released
            if(inputVal == 0)
            {
                _isLocked = false;
            }
        }

        #endregion

        #region Slow-mo

        private void ApplyCachedSlowmo()
        {
            if(_cachedIsActive && !_isActive)
            {
                ActivateSlowmo();
            }
            else if (!_cachedIsActive && _isActive)
            {
                EndSlowmo();
            }
        }

        private void ActivateSlowmo()
        {
            if(!_isActive && _canActivate && HasJuice() && !_isLocked)
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

        private void ReenableSlowmoWithParam(DynamicEvent sourceEvent, object param)
        {
            foreach (ConditionalCheck conditional in sourceEvent.Conditionals)
            {
                if (!conditional.ApplyCheck(param))
                {
                    return;
                }
            }
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

                // Lock slowmo until trigger is fully released
                _isLocked = true;
            }
        }

        private void OnCancelEventWithParam(DynamicEvent sourceEvent, object param)
        {
            foreach (ConditionalCheck conditional in sourceEvent.Conditionals)
            {
                if (!conditional.ApplyCheck(param))
                {
                    return;
                }
            }
            CancelSlowmo();
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

        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnSlowmoInputChange -= OnSlowmoInputChange;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnSlowmoInputChange += OnSlowmoInputChange;
                _isSubscribedToInput = true;
            }
        }

        private void SubscribeToEvents()
        {
            if (!_isSubscribedToEvents)
            {
                foreach(DynamicEvent cancelEvent in _cancelEvents)
                {
                    object rawObj = (object)cancelEvent.EventSource;

                    Delegate cancelHandler;

                    if (cancelEvent.EventHasParam)
                    {
                        Action<object> dynamicEvent = (param) => { OnCancelEventWithParam(cancelEvent, param); };

                        cancelHandler = Tools.WireUpEvent(rawObj, cancelEvent.EventName, dynamicEvent.Target, dynamicEvent.Method);
                    }
                    else
                    {
                        Action dynamicEvent = () => { CancelSlowmo(); };

                        cancelHandler = Tools.WireUpEvent(rawObj, cancelEvent.EventName, dynamicEvent.Target, dynamicEvent.Method);
                    }
                    cancelEvent.EventHandler = cancelHandler;
                }

                foreach (DynamicEvent reenableEvent in _reenableEvents)
                {
                    object rawObj = (object)reenableEvent.EventSource;

                    Delegate reenableHandler;

                    if (reenableEvent.EventHasParam)
                    {
                        Action<object> dynamicEvent = (param) => { ReenableSlowmoWithParam(reenableEvent, param); };

                        reenableHandler = Tools.WireUpEvent(rawObj, reenableEvent.EventName, dynamicEvent.Target, dynamicEvent.Method);
                    }
                    else
                    {
                        Action dynamicEvent = () => { ReenableSlowmo(); };

                        reenableHandler = Tools.WireUpEvent(rawObj, reenableEvent.EventName, dynamicEvent.Target, dynamicEvent.Method);
                    }
                    reenableEvent.EventHandler = reenableHandler;
                }

                _isSubscribedToEvents = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_isSubscribedToEvents)
            {
                foreach (DynamicEvent cancelEvent in _cancelEvents)
                {
                    Tools.DisconnectEvent((object)cancelEvent.EventSource, cancelEvent.EventName, cancelEvent.EventHandler);
                }

                foreach (DynamicEvent reenableEvent in _reenableEvents)
                {
                    Tools.DisconnectEvent((object)reenableEvent.EventSource, reenableEvent.EventName, reenableEvent.EventHandler);
                }

                _isSubscribedToEvents = false;
            }
        }


        #endregion

    }
}