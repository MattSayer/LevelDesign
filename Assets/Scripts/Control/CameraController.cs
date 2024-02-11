using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using Cinemachine;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace AmalgamGames.Control
{
    public class CameraController : ManagedBehaviour, IPausable
    {
        [Title("Speed")]
        [SerializeField] private float _horizontalSpeed;
        [SerializeField] private float _verticalSpeed;
        [Range(0f,1f)]
        [SerializeField] private float _burnSpeedLimit;
        [Space]
        [Title("Rotation")]
        [MinMaxSlider(-90, 90)]
        [SerializeField] private Vector2 _minMaxVerticalRotation;
        [Space]
        [Title("Distance")]
        [SerializeField] private float _zoomTime = 1;
        [SerializeField] private float _normalCamDistance = 10;
        [SerializeField] private float _chargingCamDistance = 5;
        [Space]
        [Title("Components")]
        [SerializeField] private Transform _followTarget;
        [SerializeField] private CinemachineVirtualCamera _playerCam;
        
        // STATE
        
        private float _currentHorizontalRotation = 0;
        private float _currentVerticalRotation = 0;
        
        private bool _isSubscribedToInput = false;
        private bool _isSubscribedToCharging = false;
        
        private Vector2 _currentRotationSpeed;

        private Coroutine _zoomRoutine = null;

        private bool _isActive = true;

        private bool _isBurning = false;

        // COMPONENTS
        
        private IInputProcessor _inputProcessor;
        private IRocketController _rocketController;
        private CinemachineTransposer _bodyTransposer;

        #region Lifecycle

        private void Start()
        {
            _inputProcessor = Tools.GetFirstComponentInHierarchy<IInputProcessor>(transform.parent);
            _rocketController = Tools.GetFirstComponentInHierarchy<IRocketController>(transform.parent);
            SubscribeToInput();
            SubscribeToCharging();
            _bodyTransposer = _playerCam?.GetCinemachineComponent<CinemachineTransposer>();
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
                UpdatePosition(deltaTime);
                UpdateCameraRotation(deltaTime);
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

        #region Input

        private void OnCameraInput(Vector2 rotation)
        {
            _currentRotationSpeed = rotation;
        }

        private void OnChargingStart()
        {
            if(_zoomRoutine != null)
            {
                StopCoroutine(_zoomRoutine);
            }

            _zoomRoutine = StartCoroutine(updateCameraDistance(_chargingCamDistance));
        }

        private void OnLaunch()
        {
            _isBurning = true;
        }
       
        private void OnBurnComplete()
        {
            _isBurning = false;

            if (_zoomRoutine != null)
            {
                StopCoroutine(_zoomRoutine);
            }

            _zoomRoutine = StartCoroutine(updateCameraDistance(_normalCamDistance));
        }

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

        #endregion

        #region Position/Rotation

        private void UpdatePosition(float deltaTime)
        {
            transform.position = _followTarget.position;
        }

        private void UpdateCameraRotation(float deltaTime)
        {
            float horizontalSpeed = _isBurning ? _horizontalSpeed * _burnSpeedLimit : _horizontalSpeed;
            float verticalSpeed = _isBurning ? _verticalSpeed * _burnSpeedLimit : _verticalSpeed;
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