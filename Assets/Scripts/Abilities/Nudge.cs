using AmalgamGames.Core;
using AmalgamGames.Input;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using UnityEditor.Animations;
using UnityEngine;

namespace AmalgamGames.Abilities
{
    public class Nudge : ManagedFixedBehaviour, IRespawnable, IPausable, INudger, IValueProvider, ILevelStateListener
    {

        [Title("Settings")]
        [SerializeField] private float _nudgeForce = 5000f;
        [SerializeField] private float _juiceDrainPerSecond = 10f;
        [Space]
        [Title("Components")]
        [SerializeField] private SharedFloatValue _juice;
        [SerializeField] private Rigidbody _rb;
        [Space]
        [FoldoutGroup("Dynamic Events")][SerializeField] private EventHookup[] _hookupEvents;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getInputProcessor;

        // Events
        public event Action<object> OnNudgeDirectionChanged;
        public event Action OnNudgeStart;
        public event Action OnNudgeEnd;

        // STATE

        // Subscriptions
        private bool _isSubscribedToInput = false;
        private bool _isSubscribedToEvents = false;

        // Level state
        private bool _hasLevelStarted = false;

        // Nudging
        private bool _canNudge = false;
        private Vector2 _nudgeDirection = Vector2.zero;
        private float _nudgeForceMultiplier = 1;
        private bool _isNudging = false;

        // COMPONENTS
        private IInputProcessor _inputProcessor;

        // Coroutines
        private Coroutine _nudgeMultiplierRoutine = null;

        #region Lifecycle

        protected override void OnEnable()
        {
            base.OnEnable();
            if(_hasLevelStarted)
            {
                SubscribeToInput();
                HookUpEvents();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromInput();
            UnhookEvents();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromInput();
            UnhookEvents();
        }

        public override void ManagedFixedUpdate(float deltaTime)
        {
            if(_canNudge && HasJuice() && _nudgeDirection != Vector2.zero)
            {
                float unscaledDeltaTime = Time.fixedUnscaledDeltaTime;

                // If this is the start of a new nudge, notify subscribers
                if (!_isNudging)
                {
                    OnNudgeStart?.Invoke();
                }

                OnNudgeDirectionChanged?.Invoke(_nudgeDirection);

                _isNudging = true;
                Vector3 nudgeForce = (_nudgeDirection.x * transform.right) + (_nudgeDirection.y * Vector3.up);
                
                // Max nudge magnitude is 1, since nudgeDirection is already normalised
                float nudgeMagnitude = nudgeForce.magnitude;
                _juice.SubtractValue(unscaledDeltaTime * _juiceDrainPerSecond * nudgeMagnitude);

                _rb.AddForce(_nudgeForceMultiplier * nudgeForce * _nudgeForce * unscaledDeltaTime, ForceMode.Force);
            }
            // If was nudging last frame but not now, end nudge
            else if(_isNudging)
            {
                OnNudgeDirectionChanged?.Invoke(Vector2.zero);
                _isNudging = false;
                OnNudgeEnd?.Invoke();
            }
        }

        #endregion

        #region Level State
        
        public void OnLevelStateChanged(LevelState levelState)
        {
            switch(levelState)
            {
                case LevelState.Started:
                    StartLevel();
                    break;
            }
        }
        
        private void StartLevel()
        {
            _getInputProcessor.RequestDependency(ReceiveInputProcessor);

            HookUpEvents();
            
            _hasLevelStarted = true;
        }
        
        #endregion

        #region Juice

        private bool HasJuice()
        {
            return _juice.CanSubtract(Time.unscaledDeltaTime * _juiceDrainPerSecond * _nudgeDirection.magnitude);
        }

        #endregion

        #region Nudging

        private void EnableNudging()
        {
            _canNudge = true;
        }

        private void DisableNudging()
        {
            _canNudge = false;
        }

        private void OnNudge(Vector2 nudgeDelta)
        {
            _nudgeDirection = nudgeDelta;
        }

        #endregion
        
        #region Config
        
        public void SetNudgeDrainPerSecond(float newDrain)
        {
            _juiceDrainPerSecond = newDrain;
        }
        
        public void SetNudgeForce(float newForce)
        {
            _nudgeForce = newForce;
        }
        
        #endregion

        #region Charging

        private void OnChargingStart()
        {
            DisableNudging();
        }

        private void OnLaunch(LaunchInfo launchInfo)
        {
            if(_nudgeMultiplierRoutine != null)
            {
                StopCoroutine(_nudgeMultiplierRoutine);
            }
            _nudgeMultiplierRoutine = StartCoroutine(Tools.lerpFloatOverTime(0, 1, launchInfo.BurnDuration, (value) =>
                {
                    _nudgeForceMultiplier = value;
                },
                () =>
                {
                    _nudgeMultiplierRoutine = null;
                }
            ));
            
            EnableNudging();
        }

        #endregion

        #region Respawning/Restarting

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnCollision:
                case RespawnEvent.OnRespawnStart:
                    _canNudge = false;
                    break;
            }
        }

        #endregion

        #region Pausing

        public void Pause()
        {
            gameObject.SetActive(false);
        }

        public void Resume()
        {
            gameObject.SetActive(true);
        }

        #endregion

        #region Value provider

        public void SubscribeToValue(string valueKey, Action<object> callback)
        {
            switch (valueKey)
            {
                case Globals.NUDGE_DIRECTION_CHANGED_KEY:
                    OnNudgeDirectionChanged += callback;
                    break;
            }
        }

        public void UnsubscribeFromValue(string valueKey, Action<object> callback)
        {
            switch (valueKey)
            {
                case Globals.NUDGE_DIRECTION_CHANGED_KEY:
                    OnNudgeDirectionChanged -= callback;
                    break;
            }
        }

        #endregion

        #region Subscriptions

        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnNudgeInput -= OnNudge;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnNudgeInput += OnNudge;
                _isSubscribedToInput = true;
            }
        }

        private void HookUpEvents()
        {
            if (!_isSubscribedToEvents)
            {
                Tools.HookUpEventHookups(_hookupEvents, this);
                
                _isSubscribedToEvents = true;
            }
        }

        private void UnhookEvents()
        {
            if (_isSubscribedToEvents)
            {
                Tools.UnhookEventHookups(_hookupEvents);
                _isSubscribedToEvents = false;
            }
        }

        #endregion

        #region Dependencies

        private void ReceiveInputProcessor(object rawObj)
        {
            _inputProcessor = rawObj as IInputProcessor;
            SubscribeToInput();
        }

        #endregion
    }

    public interface INudger
    {
        public event Action OnNudgeStart;
        public event Action OnNudgeEnd;
        public void SetNudgeForce(float newForce);
        public void SetNudgeDrainPerSecond(float newDrain);
    }
}