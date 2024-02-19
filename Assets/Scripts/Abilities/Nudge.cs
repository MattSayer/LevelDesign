using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace AmalgamGames.Abilities
{
    public class Nudge : ManagedFixedBehaviour, IRespawnable, INudger, IValueProvider
    {

        [Title("Settings")]
        [SerializeField] private float _nudgeForce = 1;
        [SerializeField] private float _juiceDrainPerSecond = 10f;
        [Space]
        [Title("Components")]
        [SerializeField] private Transform _rocketTransform;
        [SerializeField] private SharedFloatValue _juice;
        [SerializeField] private Rigidbody _rb;

        // Events
        public event Action<object> OnNudgeDirectionChanged;
        public event Action OnNudgeStart;
        public event Action OnNudgeEnd;

        // STATE

        // Subscriptions
        private bool _isSubscribedToInput = false;
        private bool _isSubscribedToRocket = false;

        // Nudging
        private bool _canNudge = false;
        private Vector2 _nudgeDirection = Vector2.zero;
        private float _nudgeForceMultiplier = 0;
        private bool _isNudging = false;

        // COMPONENTS
        private IInputProcessor _inputProcessor;
        private IRocketController _rocketController;

        // Coroutines
        private Coroutine _nudgeMultiplierRoutine = null;

        #region Lifecycle

        private void Start()
        {
            _inputProcessor = Tools.GetFirstComponentInHierarchy<IInputProcessor>(_rocketTransform);
            _rocketController = Tools.GetFirstComponentInHierarchy<IRocketController>(_rocketTransform);

            SubscribeToInput();
            SubscribeToRocket();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToInput();
            SubscribeToRocket();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromInput();
            UnsubscribeFromRocket();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromInput();
            UnsubscribeFromRocket();
        }

        public override void ManagedFixedUpdate(float deltaTime)
        {
            if(_canNudge && HasJuice() && _nudgeDirection != Vector2.zero)
            {
                // If this is the start of a new nudge, notify subscribers
                if(!_isNudging)
                {
                    OnNudgeStart?.Invoke();
                }

                OnNudgeDirectionChanged?.Invoke(_nudgeDirection);

                _isNudging = true;
                Vector3 nudgeForce = (_nudgeDirection.x * _rocketTransform.right) + (_nudgeDirection.y * Vector3.up);
                
                // Max nudge magnitude is 1, since nudgeDirection is already normalised
                float nudgeMagnitude = nudgeForce.magnitude;
                _juice.SubtractValue(Time.unscaledDeltaTime * _juiceDrainPerSecond * nudgeMagnitude);

                _rb.AddForce(_nudgeForceMultiplier * nudgeForce * _nudgeForce * deltaTime, ForceMode.Force);
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

        #region Juice

        private bool HasJuice()
        {
            return _juice.CanSubtract(Time.unscaledDeltaTime * _juiceDrainPerSecond * _nudgeDirection.magnitude);
        }

        #endregion

        #region Nudging

        private void OnNudge(Vector2 nudgeDelta)
        {
            _nudgeDirection = nudgeDelta;
        }

        #endregion

        #region Charging

        private void OnChargingStart(ChargingType chargingType)
        {
            _canNudge = false;
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
            _canNudge = true;
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
                _inputProcessor.OnNudgeInputChange -= OnNudge;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnNudgeInputChange += OnNudge;
                _isSubscribedToInput = true;
            }
        }

        private void SubscribeToRocket()
        {
            if(!_isSubscribedToRocket && _rocketController != null)
            {
                _rocketController.OnChargingStart += OnChargingStart;
                _rocketController.OnLaunch += OnLaunch;
            }
        }

        private void UnsubscribeFromRocket()
        {
            if (!_isSubscribedToRocket && _rocketController != null)
            {
                _rocketController.OnChargingStart -= OnChargingStart;
                _rocketController.OnLaunch -= OnLaunch;
            }
        }

        #endregion
    }

    public interface INudger
    {
        public event Action OnNudgeStart;
        public event Action OnNudgeEnd;
    }
}