using AmalgamGames.UpdateLoop;
using AmalgamGames.Visuals;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;
using AmalgamGames.Utils;
using AmalgamGames.UI;

namespace AmalgamGames.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class RocketController : ManagedFixedBehaviour, IRocketController, IPausable, IValueProvider
    {
        [Title("Charging")]
        [SerializeField] private float _chargeDeltaThreshold = 0.1f;
        [SerializeField] private float _playerChargeForce;
        [SerializeField] private float _minChargeForce;
        [Space]
        [Title("Engine Burn")]
        [SerializeField] private float _engineBurnTime = 2f;
        [SerializeField] private float _engineBurnForce = 10f;

        // EVENTS
        public event Action OnChargingStart;
        public event Action OnLaunch;
        public event Action OnBurnComplete;
        public event Action<object> OnValueChanged;

        // STATE
        private float _chargeLevel = 0;
        private bool _isSubscribedToInput = false;
        private bool _isCharging = false;
        private bool _canCharge = true;
        private bool _cachedCanCharge = true;
        private float _delayedChargeForce = 0;
        private bool _isBurning = false;
        private float _burnForce = 0;
        private bool _canLaunch = true;

        // COROUTINES
        private Coroutine _engineBurnRoutine = null;

        // COMPONENTS
        private IInputProcessor _inputProcessor;
        private ITargetOrienter _targetOrienter;
        private Rigidbody _rb;


        #region Lifecycle

        
        private void Start()
        {
            _inputProcessor = Tools.GetFirstComponentInHierarchy<IInputProcessor>(transform.parent);
            _targetOrienter = Tools.GetFirstComponentInHierarchy<ITargetOrienter>(transform.parent);
            SubscribeToInput();
            _rb = GetComponent<Rigidbody>();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromInput();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToInput();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromInput();
        }

        public override void ManagedFixedUpdate(float deltaTime)
        {
            if(_isBurning)
            {
                _rb.AddForce(transform.forward * _burnForce * deltaTime, ForceMode.Force);
            }
            OnValueChanged?.Invoke(_rb.velocity.magnitude*10);
        }

        public void ToggleLaunchAbility(bool toEnable)
        {
            _canLaunch = toEnable;
        }

        public void ToggleEnabled(bool toEnable)
        {
            enabled = toEnable;
            if (!enabled)
            {
                if (_engineBurnRoutine != null)
                {
                    StopCoroutine(_engineBurnRoutine);
                    FinishBurn();
                }
                _rb.freezeRotation = false;
                _rb.useGravity = false;
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;

                _targetOrienter.ToggleEnabled(false);
            }
            else
            {
                _canCharge = true;
                _rb.freezeRotation = true;
                _rb.useGravity = true;
                _rb.velocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _targetOrienter.ToggleEnabled(true);
                _targetOrienter.ToggleMode(OrientMode.Velocity);
            }
        }

        #endregion

        #region Pausing

        public void Pause()
        {
            _cachedCanCharge = _canCharge;

            _canCharge = false;
        }

        public void Resume()
        {
            _canCharge = _cachedCanCharge;
            // Start charge immediately if trigger was held down while game was paused
            if (_chargeLevel == 0 && _delayedChargeForce > 0)
            {
                OnSlowMoCharge(_delayedChargeForce);
                _delayedChargeForce = 0;
            }
            // If player was holding trigger when they paused, and they're still holding it now
            // Set the actual charge force to the current charge force, ignoring launch criteria
            // so it doesn't launch if they soften their pressure on the trigger while paused
            else if(_chargeLevel > 0 && _delayedChargeForce > 0)
            {
                _chargeLevel = _delayedChargeForce;
            }
        }

        #endregion

        #region Charging

        private void OnSlowMoCharge(float chargeLevel)
        {
            if (_canCharge)
            {
                // Just started charging
                if (!_isCharging && _chargeLevel == 0 && chargeLevel > 0)
                {
                    _isCharging = true;
                    OnChargingStart?.Invoke();
                    _targetOrienter.ToggleMode(OrientMode.Source);

                    Debug.Log("Charging");
                }

                float delta = _chargeLevel - chargeLevel;
                if (_canLaunch && (delta >= _chargeDeltaThreshold || (chargeLevel == 0 && _isCharging)))
                {
                    Launch();
                }
                else
                {
                    _chargeLevel = chargeLevel;
                }
            }
            else
            {
                _delayedChargeForce = chargeLevel;
            }
        }

        private void Launch()
        {
            Debug.Log("Launch");
            OnLaunch?.Invoke();
            _targetOrienter.ToggleMode(OrientMode.Velocity);

            float launchStrength = _minChargeForce + (_chargeLevel * _playerChargeForce);

            // Zero out velocity first
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

            // Launch
            _rb.AddForce(transform.forward * launchStrength, ForceMode.Impulse);

            // Disable charging for engine burn period
            _canCharge = false;
            if(_engineBurnRoutine != null)
            {
                Debug.LogError("Multiple burn routines active. This shouldn't happen");
                StopCoroutine(_engineBurnRoutine);
            }
            _engineBurnRoutine = StartCoroutine(engineBurn(_chargeLevel));

            _isCharging = false;
            _chargeLevel = 0;
        }

        private void FinishBurn()
        {
            _isBurning = false;

            // Re-enable charging
            _canCharge = true;
            OnBurnComplete?.Invoke();
            _engineBurnRoutine = null;

            // Start charge immediately if trigger was held down during burn
            if (_delayedChargeForce > 0)
            {
                OnSlowMoCharge(_delayedChargeForce);
                _delayedChargeForce = 0;
            }

            Debug.Log("Burn complete");
        }

        #endregion

        #region Coroutines

        /// <summary>
        /// Applies constant force in rocket forward direction for a predetermined burn period
        /// Player cannot start another charge while engine is burning
        /// </summary>
        /// <returns></returns>
        private IEnumerator engineBurn(float chargeLevel)
        {
            _isBurning = true;

            float initialBurnForce = _engineBurnForce * chargeLevel;

            float burnTime = _engineBurnTime * chargeLevel;
            float burnLerp = 0;

            while (burnLerp < burnTime)
            {
                _burnForce = EasingFunction.EaseInCubic(initialBurnForce, 0, burnLerp / burnTime);
                burnLerp += Time.deltaTime;
                yield return null;
            }

            FinishBurn();
        }

        #endregion

        #region Input

        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnSlowMoChargeInputChange -= OnSlowMoCharge;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnSlowMoChargeInputChange += OnSlowMoCharge;
                _isSubscribedToInput = true;
            }
        }

        #endregion
    }

    public interface IRocketController
    {
        public event Action OnChargingStart;
        public event Action OnLaunch;
        public event Action OnBurnComplete;

        public void ToggleEnabled(bool toEnable);

    }
}
