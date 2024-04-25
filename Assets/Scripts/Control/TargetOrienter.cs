using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Control
{
    public class TargetOrienter : ManagedBehaviour, IPausable, ITargetOrienter, IRespawnable
    {
        [Title("Transforms")]
        [SerializeField] private Transform _targetToOrient;
        [SerializeField] private Transform _rotationSource;
        [Space]
        [Title("Rigidbodies")]
        [SerializeField] private Rigidbody _targetRB;
        [Space]
        [Title("Settings")]
        [SerializeField] private float _rotateSpeed;
        [SerializeField] private float _minOrientVelocity = 0.707f;
        [SerializeField] private bool _useUnscaledTime = false;

        private bool _isActive = true;
        private bool _isPaused = false;
        private OrientMode _currentMode = OrientMode.Velocity;

        #region Lifecycle

        private void Start()
        {
            if (_targetRB != null)
            {
                _targetRB.freezeRotation = true;
            }
            ResetSourceToTargetRotation();
        }

        public override void ManagedUpdate(float deltaTime)
        {
            if (_isActive && !_isPaused)
            {
                float thisDelta = deltaTime;
                if (_useUnscaledTime)
                {
                    thisDelta = Time.unscaledDeltaTime;
                }

                switch(_currentMode)
                {
                    case OrientMode.Source:
                        RotateTowardsSource(thisDelta);
                        break;
                    case OrientMode.Velocity:
                        RotateTowardsVelocity(thisDelta);
                        break;
                }
            }
        }

        #endregion

        #region State

        public void ToggleMode(OrientMode newMode)
        {
            _currentMode = newMode;
        }

        public void ToggleEnabled(bool toEnable)
        {
            _isActive = toEnable;
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    ResetSourceToTargetRotation();
                    break;
            }
        }

        #endregion

        #region Rotation

        public void SetRotateSpeed(float rotateSpeed)
        {
            _rotateSpeed = rotateSpeed;
        }

        private void ResetSourceToTargetRotation()
        {
            Vector3 targetForward = _targetToOrient.forward;

            _rotationSource.forward = targetForward;
        }

        private void RotateTowardsVelocity(float deltaTime)
        {
            if (_targetRB != null)
            {
                Vector3 velocity = _targetRB.velocity;
                if (velocity.magnitude >= _minOrientVelocity)
                {
                    velocity = velocity.normalized;
                    velocity = Vector3.RotateTowards(_targetToOrient.forward, velocity, _rotateSpeed * deltaTime, 0);
                    
                    // Important! Using the rigidbody MoveRotation avoids stuttering issues with the camera that occur when modifying transform directly
                    _targetRB.MoveRotation(Quaternion.LookRotation(velocity));
                }
            }
        }

        private void RotateTowardsSource(float deltaTime)
        {
            Vector3 newRotation = Vector3.RotateTowards(_targetToOrient.forward, _rotationSource.forward, _rotateSpeed * deltaTime, 0);

            if (_targetRB != null)
            {
                // Important! Using the rigidbody MoveRotation avoids stuttering issues with the camera that occur when modifying transform directly
                _targetRB.MoveRotation(Quaternion.LookRotation(newRotation));
            }
            else
            {
                _targetToOrient.forward = newRotation;
            }
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
    }

    public enum OrientMode
    {
        Source,
        Velocity
    }

    public interface ITargetOrienter
    {
        public void ToggleMode(OrientMode newMode);
        public void ToggleEnabled(bool toActivate);
        public void SetRotateSpeed(float rotateSpeed);
    }
}