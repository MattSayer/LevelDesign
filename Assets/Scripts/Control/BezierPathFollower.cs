using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using PathCreation;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using UnityEngine;

namespace AmalgamGames.Control
{
    public class BezierPathFollower : ManagedFixedBehaviour, IRespawnable, ISpawnable, IPathFollower
    {

        [Title("Pathing")]
        [SerializeField] private PathCreator _pathCreator;
        [SerializeField] private EndOfPathInstruction _endOfPathInstruction;
        [SerializeField] private float _endOfPathDelay = 0;

        [Space]

        [Title("Movement")]
        [Unit(Units.MetersPerSecond)]
        [SerializeField] private float _moveSpeed;
        [SerializeField] private bool _moveOnStart = true;
        [SerializeField] private float _activationDelay = 0;
        [SerializeField] private EasingFunction.Ease _easingType = EasingFunction.Ease.Linear;

        [Space]

        [Title("Rotation")]
        [Unit(Units.DegreesPerSecond)]
        [SerializeField] private float _rotateSpeed;
        [SerializeField] private bool _rotateToFacePathDirection = false;
        [SerializeField] private bool _instantRotateAtEndOfPath = true;

        [Space]

        // Components
        private VertexPath _path;
        private EasingFunction.Function _easingFunction;
        private ISpawner _spawner;

        // State
        private float _progress = 0;
        private float _currentDistance = 0;
        private bool _isMovingForward = true;
        private bool _isMoving = false;
        private bool _isActive = false;
        private bool _isInitialised = false;
        private float _normalizedSpeed;

        // Coroutines
        private Coroutine _activationRoutine = null;

        // Constants
        private const EndOfPathInstruction EOP_ACTION = EndOfPathInstruction.Stop;

        #region Lifecycle

        private void Awake()
        {
            if(_pathCreator != null)
            {
                InitialisePath();
            }
        }

        public override void ManagedFixedUpdate(float deltaTime)
        {
            FollowPath(deltaTime);
        }

        #endregion

        #region Pathing

        private void InitialisePath()
        {
            _path = _pathCreator.path;
            _normalizedSpeed = 1 / (_path.length / _moveSpeed);
            _easingFunction = EasingFunction.GetEasingFunction(_easingType);
            if (_moveOnStart && _activationRoutine == null)
            {
                _activationRoutine = StartCoroutine(Tools.delayThenAction(_activationDelay, () =>
                {
                    _isActive = true;
                    _isMoving = true;
                    _activationRoutine = null;
                }));
            }
            _isInitialised = true;

        }

        public void SetPath(GameObject pathObject)
        {
            _pathCreator = pathObject.GetComponent<PathCreator>();
            InitialisePath();
        }

        private void FollowPath(float deltaTime)
        {
            // Only process when the follower is active
            if (_isActive && _isInitialised && _isMoving)
            {
                bool isAtEndOfPath = false;

                if (_isMovingForward)
                {
                    _progress += deltaTime * _normalizedSpeed;

                    // Has the follower reached the end of the path?
                    if (_progress >= 1)
                    {
                        isAtEndOfPath = true;
                        _isMoving = false;

                        StartCoroutine(Tools.delayThenAction(_endOfPathDelay, () =>
                        {
                            switch (_endOfPathInstruction)
                            {
                                case EndOfPathInstruction.Stop:
                                    return;
                                case EndOfPathInstruction.Loop:
                                    _progress = 0;
                                    break;
                                case EndOfPathInstruction.Reverse:
                                    _progress = 1;
                                    _isMovingForward = !_isMovingForward;
                                    break;
                            }
                            _isMoving = true;
                        }));
                    }
                }
                else
                {
                    _progress -= deltaTime * _normalizedSpeed;

                    // Has the follower reached the start of the path?
                    if (_progress <= 0)
                    {
                        isAtEndOfPath = true;
                        _isMoving = false;

                        StartCoroutine(Tools.delayThenAction(_endOfPathDelay, () =>
                        {
                            switch (_endOfPathInstruction)
                            {
                                case EndOfPathInstruction.Stop:
                                    return;
                                case EndOfPathInstruction.Loop:
                                    _progress = 1;
                                    break;
                                case EndOfPathInstruction.Reverse:
                                    _progress = 0;
                                    _isMovingForward = !_isMovingForward;
                                    break;
                            }
                            _isMoving = true;
                        }));
                    }
                }

                float easedProgress = _easingFunction(0, 1, _progress);
                _currentDistance = Mathf.Lerp(0, _path.length, easedProgress);

                transform.position = _path.GetPointAtTime(easedProgress, EOP_ACTION);

                if (_rotateToFacePathDirection)
                {
                    RotateToFacePathDirection(isAtEndOfPath, deltaTime);
                }
            }
        }

        #endregion

        #region Spawning

        public void Activate()
        {
            if (_activationRoutine == null)
            {
                _activationRoutine = StartCoroutine(Tools.delayThenAction(_activationDelay, () =>
                {
                    _isActive = true;
                    _isMoving = true;
                    _activationRoutine = null;
                }));
            }
        }

        public void DeactivateAndReset()
        {
            ResetPathProgress();
            _isActive = false;
        }

        public void SetSpawner(ISpawner spawner)
        {
            _spawner = spawner;
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    ResetPathProgress();
                    break;
            }
        }

        private void ResetPathProgress()
        {
            _progress = 0;
            _isMovingForward = true;
        }

        #endregion

        #region Rotation
        private void RotateToFacePathDirection(bool isAtEndOfPath, float deltaTime)
        {
            Vector3 newForward = _path.GetDirectionAtDistance(_currentDistance, EOP_ACTION);
            Vector3 newUp = _path.GetNormalAtDistance(_currentDistance, EOP_ACTION);

            // GetDirectionAtDistance returns forward travel direction, so flip it if going in reverse
            if (!_isMovingForward)
            {
                newForward *= -1;
            }

            Quaternion targetRotation = Quaternion.LookRotation(newForward, newUp);

            // Rotate follower to new direction immediately if at end of path and instant rotate is set to true
            if (isAtEndOfPath && _instantRotateAtEndOfPath)
            {
                transform.rotation = targetRotation;
            }
            // Otherwise rotate towards new forward by rotation speed
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotateSpeed * deltaTime);
            }
        }

        #endregion

        
    }
}