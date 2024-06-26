using AmalgamGames.Core;
using AmalgamGames.Effects;
using AmalgamGames.Input;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Cinemachine;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace AmalgamGames.Control
{
    public class CameraController : ManagedBehaviour, IPausable, IRespawnable, ICameraController, ILevelStateListener
    {
        [Title("Speed")]
        [SerializeField] private float _horizontalSpeed;
        [SerializeField] private float _verticalSpeed;
        [Range(0f,1f)]
        [SerializeField] private float _burnRotationSpeedLimit = 0.1f;
        [SerializeField] private float _speedLimitTransitionTime = 1;
        [SerializeField] private AnimationCurve _timeScaleSpeedMultiplier;
        [Space]
        [Title("Rotation")]
        [MinMaxSlider(-90, 90)]
        [SerializeField] private Vector2 _minMaxVerticalRotation;
        [Space]
        [Title("Distance")]
        [SerializeField] private float _zoomTime = 1;
        [SerializeField] private float _normalCamDistance = 10;
        [SerializeField] private float _chargingCamDistance = 5;
        [SerializeField] private Vector3 _chargeOffset = Vector3.zero;
        [SerializeField] private float _offsetLerpTime = 1;
        [Title("Orbit camera")]
        [SerializeField] private float _orbitDistance = 20;
        [SerializeField] private float _orbitHeight = 5;
        [SerializeField] private float _orbitRotateSpeed = 90;
        [Space]
        [Title("Damping")]
        [SerializeField] private float _launchDamping = 3;
        [Space]
        [Title("Components")]
        [SerializeField] private Transform _followTarget;
        [SerializeField] private CinemachineVirtualCamera _playerCam;
        [Space]
        [FoldoutGroup("Dynamic Events")][SerializeField] private EventHookup[] _hookupEvents;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getInputProcessor;


        // STATE

        // Rotation
        private float _currentHorizontalRotation = 0;
        private float _currentVerticalRotation = 0;
        private Vector2 _currentRotationSpeed;
        private Vector3 _initialForward;

        // Position
        private Vector3 _offset = Vector3.zero;

        // Subscriptions
        private bool _isSubscribedToInput = false;
        private bool _isSubscribedToEvents = false;

        // Coroutines
        private Coroutine _zoomRoutine = null;
        private Coroutine _speedLimitRoutine = null;
        private Coroutine _offsetRoutine = null;
        private Coroutine _dampingRoutine = null;
        private Coroutine _orbitRoutine = null;

        // Level state
        private bool _hasLevelStarted = false;

        // Respawning
        private bool _isActive = true;
        private float _postRespawnActivationDelay = 0.1f;

        // Charging
        private bool _isCharging = false;

        // Burning
        private float _rotationSpeedLimit = 1;

        // COMPONENTS
        
        private IInputProcessor _inputProcessor;
        private CinemachineTransposer _bodyTransposer;

        #region Lifecycle

        private void Start()
        {
            _bodyTransposer = _playerCam?.GetCinemachineComponent<CinemachineTransposer>();
            _initialForward = transform.forward;
            
            StartOrbitingPlayer();
        }

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

        public override void ManagedUpdate(float deltaTime)
        {
            if(_isActive && _hasLevelStarted)
            {
                UpdatePosition(Time.unscaledDeltaTime);
                UpdateCameraRotation(Time.unscaledDeltaTime * _timeScaleSpeedMultiplier.Evaluate(Time.timeScale));
            }
        }

        #endregion

        #region Pausing

        public void Pause()
        {
            _isActive = false;
        }

        public void Resume()
        {
            _isActive = true;
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
            
            StopOrbitingPlayer();
            
            _hasLevelStarted = true;
        }
        
        #endregion
        
        #region Orbit cam
        
        private void StartOrbitingPlayer()
        {
            // Set transposer distance to orbit distance
            // Start coroutine to rotate transform around Y axis
            
            _bodyTransposer.m_FollowOffset = new Vector3(0, _orbitHeight, -_orbitDistance);
            _orbitRoutine = StartCoroutine(rotateAroundPlayer());
        }
        
        private void StopOrbitingPlayer()
        {
            // Lerp transposer distance back to normal distance
            // Lerp transform forward back to initial forward
            StopCoroutine(_orbitRoutine);
            _orbitRoutine = null;
            _bodyTransposer.m_FollowOffset = new Vector3(0, 0, -_normalCamDistance);
            transform.forward = _followTarget.forward;
        }
        
        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.BeforeRespawn:
                    _playerCam.enabled = false;
                    break;
                case RespawnEvent.OnCollision:
                    OnCollision();
                    break;
                case RespawnEvent.OnRespawnStart:
                    OnRespawnStart();
                    break;
            }
            
        }

        private void OnCollision()
        {
            _isActive = false;
            ResetCoroutines();
            ResetVariables();
        }

        private void OnRespawnStart()
        {
            Invoke(nameof(DelayedActivation), _postRespawnActivationDelay);
            ResetCoroutines();
            ResetVariables();

            ResetRotation();
        }

        private void ResetCoroutines()
        {
            // Reset coroutines
            StopAllCoroutines();
            _zoomRoutine = null;
            _speedLimitRoutine = null;
            _offsetRoutine = null;
            _dampingRoutine = null;
        }

        private void ResetVariables()
        {
            // Reset variables
            _rotationSpeedLimit = 1;
            _isCharging = false;
            _offset = Vector3.zero;
        }

        public void ResetRotation()
        {
            // Reset rotation
            _currentHorizontalRotation = 0;
            _currentVerticalRotation = 0;

            Vector3 delta = transform.position - _followTarget.position;

            // Reset vcam variables
            _bodyTransposer.m_XDamping = 0;
            _bodyTransposer.m_YDamping = 0;
            _bodyTransposer.m_ZDamping = 0;
            _bodyTransposer.m_FollowOffset = new Vector3(0, 0, -_normalCamDistance);

            // Reset transform position and rotation to prevent the camera wigging out
            transform.position = _followTarget.position;
            transform.rotation = _followTarget.rotation;

            _initialForward = _followTarget.forward;

            _playerCam.OnTargetObjectWarped(transform, delta);

            _playerCam.enabled = true;

            //_zoomRoutine = StartCoroutine(updateCameraDistance(_normalCamDistance));
        }

        private void DelayedActivation()
        {
            _isActive = true;
        }

        #endregion

        #region Input

        private void OnCameraInput(Vector2 rotation)
        {
            _currentRotationSpeed = rotation;
        }

        #endregion

        #region Events

        private void OffsetAndZoomParam(object ignoreParam)
        {
            OffsetAndZoom();
        }

        private void OffsetAndZoom()
        {
            // Lerp offset to charge offset over duration

            OffsetPosition(_chargeOffset, _offsetLerpTime);

            // Reset damping

            ChangeDamping(0, 0);


            ZoomPosition(_chargingCamDistance);
        }

        private void OnLaunch(LaunchInfo launchInfo)
        {
            ChangeDamping(_launchDamping, 0, launchInfo.BurnDuration);

            OffsetPosition(Vector3.zero, _offsetLerpTime);

            _isCharging = false;

            // If the camera speed coroutine is still running, kill it since we'll create a new one after the next burn

            ChangeSpeedLimit(_burnRotationSpeedLimit, 0);

            ZoomPosition(_normalCamDistance);
        }
       
        private void RemoveBurnSpeedLimit()
        {
            ChangeSpeedLimit(1, _speedLimitTransitionTime);
        }

        public void RemoveSpeedLimit(bool removeImmediately = false)
        {
            if (removeImmediately)
            {
                if(_speedLimitRoutine != null)
                {
                    StopCoroutine(_speedLimitRoutine);
                }
                _rotationSpeedLimit = 1;
            }
            else
            {
                ChangeSpeedLimit(1, _speedLimitTransitionTime);
            }
        }

        #endregion

        #region Subscriptions

        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnCameraInput -= OnCameraInput;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _inputProcessor != null)
            { 
                _inputProcessor.OnCameraInput += OnCameraInput;
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
            if(_isSubscribedToEvents)
            {
                Tools.UnhookEventHookups(_hookupEvents);
                _isSubscribedToEvents = false;
            }
        }


        #endregion

        #region Position/Rotation

        private void UpdatePosition(float deltaTime)
        {
            transform.position = _followTarget.position + transform.TransformDirection(_offset);
        }

        private void UpdateCameraRotation(float deltaTime)
        {
            float horizontalSpeed = _horizontalSpeed * _rotationSpeedLimit;
            float verticalSpeed = _verticalSpeed * _rotationSpeedLimit;
            _currentHorizontalRotation += _currentRotationSpeed.x * deltaTime * horizontalSpeed;

            _currentVerticalRotation += _currentRotationSpeed.y * deltaTime * verticalSpeed;
            _currentVerticalRotation = Mathf.Clamp(_currentVerticalRotation, _minMaxVerticalRotation.x, _minMaxVerticalRotation.y);

            //Vector3 forward = Vector3.forward;
            Vector3 forward = _initialForward;
            forward = Quaternion.AngleAxis(_currentHorizontalRotation, Vector3.up) * forward;
            Vector3 verticalAxis = Vector3.Cross(forward, Vector3.up);
            forward = Quaternion.AngleAxis(_currentVerticalRotation, verticalAxis) * forward;
            transform.forward = forward;
        }

        private void OffsetPosition(Vector3 targetOffset, float duration)
        {
            if (_offsetRoutine != null)
            {
                StopCoroutine(_offsetRoutine);
            }
            _offsetRoutine = StartCoroutine(Tools.lerpVector3OverTimeUnscaled(_offset, targetOffset, duration, (value) =>
            {
                _offset = value;
            }, () => _offsetRoutine = null));
        }

        private void ZoomPosition(float newDistance)
        {
            if (_zoomRoutine != null)
            {
                StopCoroutine(_zoomRoutine);
            }

            _zoomRoutine = StartCoroutine(updateCameraDistance(newDistance));
        }

        #endregion

        #region Speed

        private void ChangeSpeedLimit(float targetValue, float duration)
        {
            if(_speedLimitRoutine != null)
            {
                StopCoroutine(_speedLimitRoutine);
            }

            _speedLimitRoutine = StartCoroutine(Tools.lerpFloatOverTime(_rotationSpeedLimit, targetValue, duration, (value) =>
            {
                _rotationSpeedLimit = value;
            }, () =>
            {
                _speedLimitRoutine = null;
            }));
        }
        
        public void SetHorizontalSpeed(float newSpeed)
        {
            _horizontalSpeed = newSpeed;
        }
        
        public void SetVerticalSpeed(float newSpeed)
        {
            _verticalSpeed = newSpeed;
        }

        #endregion

        #region Camera specific

        private void ChangeDamping(float targetValue, float duration)
        {
            // Get current value
            float startValue = _bodyTransposer.m_XDamping;

            ChangeDamping(startValue, targetValue, duration);
        }

        private void ChangeDamping(float startValue, float targetValue, float duration)
        {
            if(_dampingRoutine != null)
            {
                StopCoroutine(_dampingRoutine);
            }

            _dampingRoutine = StartCoroutine(Tools.lerpFloatOverTime(startValue, targetValue, duration, (value) =>
            {
                _bodyTransposer.m_XDamping = value;
                _bodyTransposer.m_YDamping = value;
                _bodyTransposer.m_ZDamping = value;
            },
            () =>
            {
                _dampingRoutine = null;
            }));
        }

        #endregion

        #region Dependencies

        private void ReceiveInputProcessor(object rawObj)
        {
            _inputProcessor = rawObj as IInputProcessor;
            SubscribeToInput();
        }

        #endregion

        #region Coroutines

        private IEnumerator updateCameraDistance(float newDistance)
        {
            float zoomLerp = 0;

            if (_bodyTransposer != null)
            {
                float currentDistance = _bodyTransposer.m_FollowOffset.z;

                while (zoomLerp < _zoomTime)
                {
                    float dist = Mathf.Lerp(currentDistance, -newDistance, zoomLerp / _zoomTime);
                    _bodyTransposer.m_FollowOffset = new Vector3(0, 0, dist);
                    zoomLerp += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            _zoomRoutine = null;
        }
        
        private IEnumerator rotateAroundPlayer()
        {
            while(!_hasLevelStarted) 
            {
                transform.Rotate(Vector3.up * _orbitRotateSpeed * Time.deltaTime);
                yield return null;
            }
        }

        #endregion
    }

    public interface ICameraController
    {
        public void RemoveSpeedLimit(bool removeImmediately = false);
        public void ResetRotation();
        public void SetHorizontalSpeed(float newSpeed);
        public void SetVerticalSpeed(float newSpeed);
    }
}