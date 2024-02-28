using AmalgamGames.UpdateLoop;
using AmalgamGames.Control;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AmalgamGames.Utils;
using AmalgamGames.UI;
using AmalgamGames.Config;
using System.Linq;

namespace AmalgamGames.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class RocketController : ManagedFixedBehaviour, IRocketController, IPausable, IValueProvider, IRespawnable
    {
        [Title("Config")]
        [SerializeField] private RocketConfig _config;
        
        // EVENTS
        public event Action OnChargingStart;
        public event Action<LaunchInfo> OnLaunch;
        public event Action OnBurnComplete;
        public event Action<object> OnVelocityChanged;
        public event Action<object> OnChargeLevelChanged;

        // Config
        private float _chargeDeltaThreshold = 0.1f;
        private float _playerChargeForce;
        private float _minChargeForce;
        private float _minEngineBurnTime = 1f;
        private float _engineBurnTime = 2f;
        private float _engineBurnForce = 10f;

        // STATE

        // Subscriptions
        private bool _isSubscribedToInput = false;
        
        // Launch
        private bool _canLaunch = true;

        // Charging
        private float _chargeLevel = 0;
        private bool _isCharging = false;
        private bool _canCharge = true;
        private bool _cachedCanCharge = true;

        // Cached charging
        private float _cachedChargeLevel = 0;
        private bool _runDelayedChargeCheck = false;

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
            LoadConfig();

            _inputProcessor = Tools.GetFirstComponentInHierarchy<IInputProcessor>(transform.parent);
            _targetOrienter = Tools.GetFirstComponentInHierarchy<ITargetOrienter>(transform.parent);
            SubscribeToInput();
            _rb = GetComponent<Rigidbody>();

            // No gravity on level start, will reactivate on launch
            _rb.useGravity = false;
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
                //Debug.Log("actual position: " + transform.position);
            }

            // TODO Normalize value based on max velocity
            OnVelocityChanged?.Invoke(_rb.velocity.magnitude*10);

            if(_runDelayedChargeCheck)
            {
                _runDelayedChargeCheck = false;
                CheckDelayedChargeForce();
            }
        }

        public void ToggleLaunchAbility(bool toEnable)
        {
            _canLaunch = toEnable;
        }

        #endregion

        #region Config

        private void LoadConfig()
        {
            _chargeDeltaThreshold = _config.ChargeDeltaThreshold;
            _playerChargeForce = _config.PlayerChargeForce;
            _minChargeForce = _config.MinChargeForce;
            _minEngineBurnTime = _config.MinEngineBurnTime;
            _engineBurnTime = _config.EngineBurnTime;
            _engineBurnForce = _config.EngineBurnForce;
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
            }

            _isBurning = false;
        }

        private void CacheChargeLevel()
        {
            if(_cachedChargeLevel == 0)
            {
                _cachedChargeLevel = _chargeLevel;
            }
        }

        private void ResetChargeState()
        {
            _isCharging = false;
            _chargeLevel = 0;
            OnChargeLevelChanged?.Invoke(_chargeLevel);
        }

        private void DisableRocket()
        {
            CacheChargeLevel();

            ResetChargeState();
            
            StopBurnRoutine();
            _rb.useGravity = false;
            SetVelocityToZero();

            _canCharge = false;
        }

        private void ResetRocket()
        {
            CacheChargeLevel();

            ResetChargeState();

            StopBurnRoutine();
            _rb.useGravity = false;
            SetVelocityToZero();

            _canCharge = true;
            _canLaunch = false;

            _runDelayedChargeCheck = true;
        }

        private void RestartRocket()
        {
            _canLaunch = true;
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
            CheckDelayedChargeForce();
        }
        
        private void CheckDelayedChargeForce()
        {
            // Start charge immediately if trigger was held down while game was paused
            if (!_isCharging && _cachedChargeLevel > 0)
            {
                OnChargeInputChange(_cachedChargeLevel);
            }
            // If player was holding trigger when they paused, and they're still holding it now
            // Set the actual charge force to the current charge force, ignoring launch criteria
            // so it doesn't launch if they soften their pressure on the trigger while paused
            else if (_isCharging && _cachedChargeLevel > 0)
            {
                _chargeLevel = _cachedChargeLevel;
                OnChargeLevelChanged?.Invoke(_chargeLevel);
            }
            else if(_isCharging)
            {
                if(_canLaunch)
                {
                    Launch();
                }
                else
                {
                    Debug.LogError("Delayed release should have launched.");
                    _chargeLevel = 0;
                }
            }

            // Clear cached charge level
            _cachedChargeLevel = 0;
        }

        #endregion

        #region Charging

        private void OnChargeInputChange(float chargeLevel)
        {
            if (_canCharge)
            {
                // Just started charging
                if (!_isCharging && _chargeLevel == 0 && chargeLevel > 0)
                {
                    _isCharging = true;
                    OnChargingStart?.Invoke();

                    _targetOrienter?.ToggleMode(OrientMode.Source);

                    //Debug.Log("Charging");
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
                _cachedChargeLevel = chargeLevel;

                //Debug.Log($"Delayed charging: {chargeLevel}");
            }
        }

        public void Launch()
        {
            if (_isCharging)
            {
                //Debug.Log("Launch: " + _chargeLevel);

                // Reactivate gravity if it was disabled
                _rb.useGravity = true;

                float engineBurnTime = Mathf.Max(_minEngineBurnTime, _engineBurnTime * _chargeLevel);

                LaunchInfo launchInfo = new LaunchInfo(_chargeLevel, engineBurnTime);

                TriggerLaunchEvent(launchInfo);

                _targetOrienter?.ToggleMode(OrientMode.Velocity);

                float launchStrength = _minChargeForce + (_chargeLevel * _playerChargeForce);

                SetVelocityToZero();

                // Launch
                _rb.AddForce(transform.forward * launchStrength, ForceMode.Impulse);

                // Disable charging for engine burn period
                _canCharge = false;
                if (_engineBurnRoutine != null)
                {
                    Debug.LogError("Multiple burn routines active. This shouldn't happen");
                    StopCoroutine(_engineBurnRoutine);
                }
                _engineBurnRoutine = StartCoroutine(engineBurn(launchInfo));

                _isCharging = false;
                _chargeLevel = 0;
                OnChargeLevelChanged?.Invoke(_chargeLevel);
            }
        }

        private void FinishBurn()
        {
            _isBurning = false;

            // Re-enable charging
            _canCharge = true;
            OnBurnComplete?.Invoke();
            _engineBurnRoutine = null;

            // Start charge immediately if trigger was held down during burn
            CheckDelayedChargeForce();

            //Debug.Log("Burn complete");
        }

        private void TriggerLaunchEvent(LaunchInfo launchInfo)
        {
            OnLaunch?.Invoke(launchInfo);
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
                _inputProcessor.OnChargeInputChange -= OnChargeInputChange;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnChargeInputChange += OnChargeInputChange;
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

    public class ChargingInfo
    {
        public ChargingType chargingType;

        public ChargingInfo(ChargingType chargingType)
        {
            this.chargingType = chargingType;
        }
    }

    public enum ChargingType
    {
        Slowmo,
        Realtime
    }

    public interface IRocketController
    {
        public event Action OnChargingStart;
        public event Action<LaunchInfo> OnLaunch;
        public event Action OnBurnComplete;
    }
}
