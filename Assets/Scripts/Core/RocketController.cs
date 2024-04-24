using AmalgamGames.UpdateLoop;
using AmalgamGames.Control;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;
using AmalgamGames.Utils;
using AmalgamGames.UI;
using AmalgamGames.Config;

namespace AmalgamGames.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class RocketController : ManagedFixedBehaviour, IRocketController, IPausable, IValueProvider, IRespawnable, ILevelStateListener
    {
        [Title("Config")]
        [SerializeField] private RocketConfig _config;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getInputProcessor;

        // EVENTS
        public event Action OnChargingStart;
        public event Action<LaunchInfo> OnLaunch;
        public event Action OnBurnComplete;
        public event Action<object> OnVelocityChanged;
        public event Action<object> OnChargeLevelChanged;
        public event Action<object> OnNumLaunchesChanged;

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
        
        // Level state
        private bool _hasLevelStarted = false;
        
        // Launch
        private bool _canLaunch = true;
        private float _canLaunchTimestamp = 0;
        private int _numLaunches = 0;

        // Charging
        private float _chargeLevel = 0;
        private bool _isCharging = false;
        private bool _canCharge = true;
        private bool _cachedCanCharge = true;

        // Cached charging
        private float _cachedChargeLevel = 0;
        private bool _runDelayedChargeCheck = false;

        // Buffered charging
        private float[] _chargeBuffer = new float[CHARGE_BUFFER_SIZE];

        // Burning
        private bool _isBurning = false;
        private float _burnForce = 0;
        
        // COROUTINES
        private Coroutine _engineBurnRoutine = null;

        // COMPONENTS
        private IInputProcessor _inputProcessor;
        private ITargetOrienter _targetOrienter;
        private Rigidbody _rb;

        // CONSTANTS
        private const float METRES_SECOND_TO_KILOMETERS_HOUR = 3.6f;
        private const int CHARGE_BUFFER_SIZE = 10;
        private const float JUST_LAUNCHED_BUFFER_TIME = 0.1f;

        #region Lifecycle

        
        private void Start()
        {
            _targetOrienter = Tools.GetFirstComponentInHierarchy<ITargetOrienter>(transform.parent);
            
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
            if(_hasLevelStarted)
            {
                SubscribeToInput();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromInput();
        }

        public override void ManagedFixedUpdate(float deltaTime)
        {
            if(_hasLevelStarted)
            {
                if(_isBurning)
                {
                    _rb.AddForce(transform.forward * _burnForce * deltaTime, ForceMode.Force);
                }

                // TODO Normalize value based on max velocity
                OnVelocityChanged?.Invoke(_rb.velocity.magnitude*METRES_SECOND_TO_KILOMETERS_HOUR);

                if(_runDelayedChargeCheck)
                {
                    _runDelayedChargeCheck = false;
                    CheckDelayedChargeForce();
                }
            }
        }

        #endregion

        #region Level state

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
            LoadConfig();

            _getInputProcessor.RequestDependency(ReceiveInputProcessor);
            
            _hasLevelStarted = true;
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
        
        public void SetConfig(RocketConfig config)
        {
            _config = config;
            LoadConfig();
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

            //OnBurnComplete?.Invoke();

            _isBurning = false;
        }

        private void NotifyBurnComplete()
        {
            OnBurnComplete?.Invoke();
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
            TriggerChargeEvent();
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
            ToggleLaunchAbility(false);

            _runDelayedChargeCheck = true;
        }

        private void RestartRocket()
        {
            ToggleLaunchAbility(true);
        }

        public void EnableImmediateLaunch()
        {
            CacheChargeLevel();

            ResetChargeState();

            StopBurnRoutine();
            NotifyBurnComplete();

            _canCharge = true;
            ToggleLaunchAbility(false);

            _runDelayedChargeCheck = true;
            ToggleLaunchAbility(true);
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
                TriggerChargeEvent();
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
                }

                float delta = _chargeLevel - chargeLevel;
                
                // If launching was just enabled, consider buffered charging values when calculating delta
                if(Time.time - _canLaunchTimestamp < JUST_LAUNCHED_BUFFER_TIME)
                {
                    float maxChargeBuffer = GetMaxChargeBufferValue();
                    delta = Mathf.Max(delta, maxChargeBuffer - chargeLevel);
                    // Also set charge level to the max buffered charge level
                    _chargeLevel = maxChargeBuffer;
                }

                if (_canLaunch && (delta >= _chargeDeltaThreshold || (chargeLevel == 0 && _isCharging)))
                {
                    Launch();
                }
                else
                {
                    _chargeLevel = chargeLevel;
                    TriggerChargeEvent();
                }
            }
            else
            {
                _cachedChargeLevel = chargeLevel;
            }

            AppendToChargeBuffer(chargeLevel);
        }

        private void AppendToChargeBuffer(float chargeLevel)
        {
            for(int i = CHARGE_BUFFER_SIZE - 1; i > 0; i--)
            {
                _chargeBuffer[i] = _chargeBuffer[i - 1];
            }
            _chargeBuffer[0] = chargeLevel;
        }

        /// <summary>
        /// Gets the max delta between the provided charge level and the cached charge levels
        /// in the charging buffer. This accounts for when the player releases the trigger just 
        /// before launching is enabled (i.e. during the launch countdown) so they still get a proper
        /// launch
        /// </summary>
        /// <param name="chargeLevel"></param>
        /// <returns></returns>
        private float GetMaxChargeBufferValue()
        {
            float maxValue = 0;
            for(int i = 0; i < CHARGE_BUFFER_SIZE; i++)
            {
                maxValue = Mathf.Max(maxValue, _chargeBuffer[i]);
            }
            return maxValue;
        }



        public void Launch()
        {
            if (_isCharging)
            {
                // Reactivate gravity if it was disabled
                _rb.useGravity = true;

                float engineBurnTime = Mathf.Max(_minEngineBurnTime, _engineBurnTime * _chargeLevel);

                LaunchInfo launchInfo = new LaunchInfo(_chargeLevel, engineBurnTime);

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
                TriggerChargeEvent();

                // Triggering launch event after charge event, in case something is
                // listening to both
                TriggerLaunchEvent(launchInfo);

                _numLaunches++;
                OnNumLaunchesChanged?.Invoke(_numLaunches);
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
        }

        private void TriggerLaunchEvent(LaunchInfo launchInfo)
        {
            OnLaunch?.Invoke(launchInfo);
        }

        private void TriggerChargeEvent()
        {
            OnChargeLevelChanged?.Invoke(_chargeLevel);
        }

        #endregion

        #region Launching

        public void ToggleLaunchAbility(bool toEnable)
        {
            _canLaunch = toEnable;
            if(_canLaunch)
            {
                _canLaunchTimestamp = Time.time;
            }
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
                case Globals.NUM_LAUNCHES_KEY:
                    OnNumLaunchesChanged += callback;
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
                    OnChargeLevelChanged -= callback;
                    break;
                case Globals.NUM_LAUNCHES_KEY:
                    OnNumLaunchesChanged -= callback;
                    break;
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

        public void EnableImmediateLaunch();
        public void SetConfig(RocketConfig config);
    }
}
