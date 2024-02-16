using AmalgamGames.Core;
using AmalgamGames.Effects;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Cinemachine;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace AmalgamGames.Control
{
    public class CameraController : ManagedBehaviour, IPausable, IRespawnable
    {
        [Title("Speed")]
        [SerializeField] private float _horizontalSpeed;
        [SerializeField] private float _verticalSpeed;
        [Range(0f,1f)]
        [SerializeField] private float _burnRotationSpeedLimit = 0.1f;
        [SerializeField] private float _speedLimitTransitionTime = 1;
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
        [Space]
        [Title("Damping")]
        [SerializeField] private float _launchDamping = 3;
        [Space]
        [Title("Screen shake")]
        [SerializeField] private float _launchShakeAmplitude = 10f;
        [SerializeField] private float _launchShakeFrequency = 1;
        [SerializeField] private float _launchShakeDuration = 0.5f;
        [SerializeField] private EasingFunction.Ease _launchShakeEasing = EasingFunction.Ease.Linear;
        [Space]
        [Title("Components")]
        [SerializeField] private Transform _followTarget;
        [SerializeField] private CinemachineVirtualCamera _playerCam;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getScreenShaker;
        
        // STATE
        
        // Rotation
        private float _currentHorizontalRotation = 0;
        private float _currentVerticalRotation = 0;
        private Vector2 _currentRotationSpeed;

        // Position
        private Vector3 _offset = Vector3.zero;

        // Subscriptions
        private bool _isSubscribedToInput = false;
        private bool _isSubscribedToCharging = false;

        // Coroutines
        private Coroutine _zoomRoutine = null;
        private Coroutine _speedLimitRoutine = null;
        private Coroutine _offsetRoutine = null;
        private Coroutine _dampingRoutine = null;

        // Active
        private bool _isActive = true;

        // Charging
        private bool _isCharging = false;

        // Burning
        private bool _isBurning = false;
        private float _rotationSpeedLimit = 1;

        // COMPONENTS
        
        private IInputProcessor _inputProcessor;
        private IRocketController _rocketController;
        private CinemachineTransposer _bodyTransposer;
        private IScreenShaker _screenShaker;

        #region Lifecycle

        private void Start()
        {
            _inputProcessor = Tools.GetFirstComponentInHierarchy<IInputProcessor>(transform.parent);
            _rocketController = Tools.GetFirstComponentInHierarchy<IRocketController>(transform.parent);
            _bodyTransposer = _playerCam?.GetCinemachineComponent<CinemachineTransposer>();

            _getScreenShaker.RequestDependency(ReceiveScreenShaker);

            SubscribeToInput();
            SubscribeToCharging();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToInput();
            SubscribeToCharging();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromInput();
            UnsubscribeFromCharging();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromInput();
            UnsubscribeFromCharging();
        }

        public override void ManagedUpdate(float deltaTime)
        {
            if(_isActive)
            {
                UpdatePosition(Time.unscaledDeltaTime);
                UpdateCameraRotation(Time.unscaledDeltaTime);
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

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    OnRespawnStart();
                    break;
            }
            
        }

        private void OnRespawnStart()
        {
            // Reset coroutines
            StopAllCoroutines();
            _zoomRoutine = null;
            _speedLimitRoutine = null;
            _offsetRoutine = null;

            // Reset variables
            _rotationSpeedLimit = 1;
            _isBurning = false;
            _isCharging = false;
            _isActive = true;
            _offset = Vector3.zero;
        }

        #endregion

        #region Input

        private void OnCameraInput(Vector2 rotation)
        {
            _currentRotationSpeed = rotation;
        }

        private void OnChargingStart(ChargingType chargingType)
        {
            // Lerp offset to charge offset over duration
            
            if(_offsetRoutine != null)
            {
                StopCoroutine(_offsetRoutine);
            }
            _offsetRoutine = StartCoroutine(Tools.lerpVector3OverTime(_offset, _chargeOffset, _offsetLerpTime, (value) =>
            {
                _offset = value;
            },() => _offsetRoutine = null));

            if (_zoomRoutine != null)
            {
                StopCoroutine(_zoomRoutine);
            }

            // Reset damping

            if (_dampingRoutine != null)
            {
                StopCoroutine(_dampingRoutine);
                _dampingRoutine = null;
            }
            _bodyTransposer.m_XDamping = 0;
            _bodyTransposer.m_YDamping = 0;
            _bodyTransposer.m_ZDamping = 0;

            _zoomRoutine = StartCoroutine(updateCameraDistance(_chargingCamDistance));
        }

        private void OnLaunch(LaunchInfo launchInfo)
        {
            // screen shake
            _screenShaker.ScreenShakeBurst(new ScreenShakeIntensity(_launchShakeAmplitude, _launchShakeFrequency), launchInfo.BurnDuration, _launchShakeEasing);
            
            // Increase damping temporarily
            if (_dampingRoutine == null)
            {
                _dampingRoutine = StartCoroutine(Tools.lerpFloatOverTime(_launchDamping, 0, launchInfo.BurnDuration, (value) =>
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
            else
            {
                Debug.LogError("Damping routine not null. This shouldn't happen");
            }

            if (_offsetRoutine != null)
            {
                StopCoroutine(_offsetRoutine);
            }
            // Lerp offset to zero over duration
            _offsetRoutine = StartCoroutine(Tools.lerpVector3OverTime(_offset, Vector3.zero, _offsetLerpTime, (value) =>
            {
                _offset = value;
            }, () => _offsetRoutine = null));

            _isCharging = false;
            _isBurning = true;
            
            // If the camera speed coroutine is still running, kill it since we'll create a new one after the next burn
            if(_speedLimitRoutine != null)
            {
                StopCoroutine(_speedLimitRoutine);
                _speedLimitRoutine = null;
            }
            _rotationSpeedLimit = _burnRotationSpeedLimit;

            if (_zoomRoutine != null)
            {
                StopCoroutine(_zoomRoutine);
            }

            _zoomRoutine = StartCoroutine(updateCameraDistance(_normalCamDistance));
        }
       
        private void OnBurnComplete()
        {
            _isBurning = false;
            
            if (_speedLimitRoutine == null)
            {
                _speedLimitRoutine = StartCoroutine(Tools.lerpFloatOverTime(_rotationSpeedLimit, 1, _speedLimitTransitionTime, (value) =>
                {
                    _rotationSpeedLimit = value;
                }, () =>
                {
                    _speedLimitRoutine = null;
                }));
            }
            else
            {
                Debug.LogError("Speed limit routine not null. This shouldn't happen");
            }
            
        }

        #endregion

        #region Subscriptions

        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnCameraInputChange -= OnCameraInput;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _inputProcessor != null)
            { 
                _inputProcessor.OnCameraInputChange += OnCameraInput;
                _isSubscribedToInput = true;
            }
        }

        private void SubscribeToCharging()
        {
            if (!_isSubscribedToCharging && _rocketController != null)
            {
                _rocketController.OnChargingStart += OnChargingStart;
                _rocketController.OnLaunch += OnLaunch;
                _rocketController.OnBurnComplete += OnBurnComplete;
                _isSubscribedToCharging = true;
            }
        }

        private void UnsubscribeFromCharging()
        {
            if (_isSubscribedToCharging && _rocketController != null)
            {
                _rocketController.OnChargingStart -= OnChargingStart;
                _rocketController.OnLaunch -= OnLaunch;
                _rocketController.OnBurnComplete -= OnBurnComplete;
                _isSubscribedToCharging = false;
            }
        }

        private void ReceiveScreenShaker(object rawObj)
        {
            _screenShaker = rawObj as IScreenShaker;
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

            Vector3 forward = Vector3.forward;
            forward = Quaternion.AngleAxis(_currentHorizontalRotation, Vector3.up) * forward;
            Vector3 verticalAxis = Vector3.Cross(forward, Vector3.up);
            forward = Quaternion.AngleAxis(_currentVerticalRotation, verticalAxis) * forward;
            transform.forward = forward;
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

        #endregion
    }
}