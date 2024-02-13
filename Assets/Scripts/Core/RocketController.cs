using AmalgamGames.UpdateLoop;
using AmalgamGames.Control;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;
using AmalgamGames.Utils;
using AmalgamGames.UI;
using UnityEngine.Rendering;

namespace AmalgamGames.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class RocketController : ManagedFixedBehaviour, IRocketController, IPausable, IValueProvider, IRespawnable
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
        public event Action<ChargingType> OnChargingStart;
        public event Action<LaunchInfo> OnLaunch;
        public event Action OnBurnComplete;
        public event Action<object> OnVelocityChanged;
        public event Action<object> OnChargeLevelChanged;

        // STATE
        
        // Subscriptions
        private bool _isSubscribedToInput = false;
        
        // Launch
        private bool _canLaunch = true;

        // Charging
        private float _chargeLevel = 0;
        private ChargingType _chargingType;
        private bool _isCharging = false;
        private bool _canCharge = true;
        private bool _cachedCanCharge = true;
        private ChargingType _delayedChargingType;
        private float _delayedChargeForce = 0;
        
        // Burning
        private bool _isBurning = false;
        private float _burnForce = 0;
        

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

            // TODO Normalize value based on max velocity
            OnVelocityChanged?.Invoke(_rb.velocity.magnitude*10);
        }

        public void ToggleLaunchAbility(bool toEnable)
        {
            _canLaunch = toEnable;
        }

        #endregion

        #region Respawning/restarting

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnCollision:
                    DisableRocket();
                    break;
                case RespawnEvent.OnRespawnStart:
                    ResetRocket();
                    break;
                case RespawnEvent.OnRespawnEnd:
                    RestartRocket();
                    break;
            }
        }

        private void SetVelocityToZero()
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        private void StopBurnRoutine()
        {
            if (_engineBurnRoutine != null)
            {
                StopCoroutine(_engineBurnRoutine);
                _engineBurnRoutine = null;
                FinishBurn();
            }
        }

        private void ResetChargeState()
        {
            _isCharging = false;
            _chargeLevel = 0;
        }

        private void DisableRocket()
        {
            ResetChargeState();
            
            StopBurnRoutine();
            _rb.useGravity = false;
            SetVelocityToZero();

            _canCharge = false;
        }

        private void ResetRocket()
        {
            ResetChargeState();

            StopBurnRoutine();
            _rb.useGravity = false;
            SetVelocityToZero();

            _canCharge = true;
            _canLaunch = false;
        }

        private void RestartRocket()
        {
            StopBurnRoutine();
            _rb.useGravity = true;
            SetVelocityToZero();

            _canLaunch = true;
            _canCharge = true;

            _targetOrienter.ToggleMode(OrientMode.Velocity);
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
                OnCharge(_delayedChargingType, _delayedChargeForce);
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

        private void OnSlowmoCharge(float chargeLevel)
        {
            OnCharge(ChargingType.Slowmo, chargeLevel);
        }

        private void OnRealtimeCharge(float chargeLevel)
        {
            OnCharge(ChargingType.Realtime, chargeLevel);
        }

        private void OnCharge(ChargingType chargingType, float chargeLevel)
        {
            // If player is already charging one trigger and pulls the other trigger, just ignore it
            if(_isCharging && _chargingType != chargingType)
            {
                return;
            }

            if (_canCharge)
            {
                // Just started charging
                if (!_isCharging && _chargeLevel == 0 && chargeLevel > 0)
                {
                    _isCharging = true;
                    _chargingType = chargingType;
                    OnChargingStart?.Invoke(chargingType);

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
                OnChargeLevelChanged?.Invoke(_chargeLevel);
            }
            else
            {
                _delayedChargingType = chargingType;
                _delayedChargeForce = chargeLevel;
            }
        }

        private void Launch()
        {
            Debug.Log("Launch");

            float engineBurnTime = _engineBurnTime * _chargeLevel;

            LaunchInfo launchInfo = new LaunchInfo(_chargeLevel, engineBurnTime);

            OnLaunch?.Invoke(launchInfo);

            _targetOrienter.ToggleMode(OrientMode.Velocity);

            float launchStrength = _minChargeForce + (_chargeLevel * _playerChargeForce);

            SetVelocityToZero();

            // Launch
            _rb.AddForce(transform.forward * launchStrength, ForceMode.Impulse);

            // Disable charging for engine burn period
            _canCharge = false;
            if(_engineBurnRoutine != null)
            {
                Debug.LogError("Multiple burn routines active. This shouldn't happen");
                StopCoroutine(_engineBurnRoutine);
            }
            _engineBurnRoutine = StartCoroutine(engineBurn(launchInfo));

            _isCharging = false;
            _chargeLevel = 0;
            OnChargeLevelChanged?.Invoke(_chargeLevel);
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
                OnSlowmoCharge(_delayedChargeForce);
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
        private IEnumerator engineBurn(LaunchInfo launchInfo)
        {
            _isBurning = true;

            float initialBurnForce = _engineBurnForce * launchInfo.ChargeLevel;

            float burnLerp = 0;

            while (burnLerp < launchInfo.BurnDuration)
            {
                _burnForce = EasingFunction.EaseInCubic(initialBurnForce, 0, burnLerp / launchInfo.BurnDuration);
                burnLerp += Time.deltaTime;
                yield return null;
            }

            FinishBurn();
        }

        #endregion

        #region Value provider

        public void SubscribeToValue(string valueKey, Action<object> callback)
        {
            switch(valueKey)
            {
                case Globals.VELOCITY_CHANGED_KEY:
                    OnVelocityChanged += callback;
                    break;
                case Globals.CHARGE_LEVEL_CHANGED_KEY:
                    OnChargeLevelChanged += callback;
                    break;
            }
        }

        public void UnsubscribeFromValue(string valueKey, Action<object> callback)
        {
            switch(valueKey)
            {
                case Globals.VELOCITY_CHANGED_KEY:
                    OnVelocityChanged -= callback;
                    break;
                case Globals.CHARGE_LEVEL_CHANGED_KEY:
                    OnChargeLevelChanged += callback;
                    break;
            }
        }

        #endregion

        #region Input

        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnSlowMoChargeInputChange -= OnSlowmoCharge;
                _inputProcessor.OnRealtimeChargeInputChange -= OnRealtimeCharge;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnSlowMoChargeInputChange += OnSlowmoCharge;
                _inputProcessor.OnRealtimeChargeInputChange += OnRealtimeCharge;
                _isSubscribedToInput = true;
            }
        }

        #endregion
    }

    public class LaunchInfo
    {
        public float ChargeLevel;
        public float BurnDuration;

        public LaunchInfo(float chargeLevel, float burnDuration)
        {
            ChargeLevel = chargeLevel;
            BurnDuration = burnDuration;
        }
    }

    public enum ChargingType
    {
        Slowmo,
        Realtime
    }

    public interface IRocketController
    {
        public event Action<ChargingType> OnChargingStart;
        public event Action<LaunchInfo> OnLaunch;
        public event Action OnBurnComplete;
    }
}
