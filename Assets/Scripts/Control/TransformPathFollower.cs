using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using PathCreation;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Technie.PhysicsCreator;
using UnityEngine;

namespace AmalgamGames.Control
{
    public class TransformPathFollower : ManagedFixedBehaviour, IRespawnable, ITriggerable, IPathFollower, ISpawnable
    {
        [Title("Path")]
        [SerializeField] private Transform _pathParent;
        [SerializeField] private EndOfPathInstruction _endOfPathInstruction;
        [SerializeField] private float _pointWaitTime = 0;
        [SerializeField] private bool _isClosedPath = false;
        [SerializeField] private bool _useDynamicPoints = false;
        [Range(0f,1f)]
        [SerializeField] private float _pathStartPosition = 0;
        [SerializeField] private PathDirection _startDirection = PathDirection.Forward;

        [Space]
        
        [Title("Movement")]
        [Unit(Units.MetersPerSecond)]
        [SerializeField] private float _moveSpeed;
        [SerializeField] private bool _moveOnStart = true;
        [SerializeField] private float _activationDelay = 0;
        [SerializeField] private EasingFunction.Ease _easingType = EasingFunction.Ease.Linear;

        [Space]

        [Title("Respawning")]
        [SerializeField] private bool _handleRespawnEvents = true;

        [Space]
        
        [Title("Rotation")]
        [SerializeField] private bool _rotateToFacePathDirection = false;

        // State
        private int _nextPointIndex = 1;
        private float _progress = 0;
        private bool _isActive = false;
        private bool _isMoving = false;
        private bool _isMovingForward = true;
        private float _currentSpeed;
        private bool _pathInitialised = false;

        // Path
        private EasingFunction.Function _easingFunction;
        private float _totalPathDistance;
        private float _totalPathTime;
        private Transform[] _pointTransforms;
        private Vector3[] _points;
        private int _pathSize;

        // Spawning
        private ISpawner _spawner;

        // Coroutines
        private Coroutine _waitRoutine = null;
        private Coroutine _activationRoutine = null;

        #region Lifecycle

        private void Awake()
        {
            _easingFunction = EasingFunction.GetEasingFunction(_easingType);

            if (_pathParent != null)
            {
                InitialisePath();
            }
        }

        private void Start()
        {
            if (_moveOnStart && _pathInitialised)
            {
                Activate();
            }
        }

        public override void ManagedFixedUpdate(float deltaTime)
        {
            FollowPath(deltaTime);
        }

        #endregion

        #region Spawning

        public void DeactivateAndReset()
        {
            ResetPathProgress();
            _isActive = false;
        }

        public void Activate()
        {
            if (_activationRoutine == null)
            {
                _activationRoutine = StartCoroutine(Tools.delayThenAction(_activationDelay, () =>
                {
                    _isActive = true;
                    StartMoving();
                    _activationRoutine = null;
                }));
            }
        }

        public void SetSpawner(ISpawner spawner)
        {
            _spawner = spawner;
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            if (_handleRespawnEvents)
            {
                switch (evt)
                {
                    case RespawnEvent.OnRespawnStart:
                        DeactivateAndReset();
                        CheckMoveOnStart();
                        break;
                }
            }
        }

        private void ResetPathProgress()
        {
            if (_waitRoutine != null)
            {
                StopCoroutine(_waitRoutine);
                _waitRoutine = null;
            }

            if(_activationRoutine != null)
            {
                StopCoroutine( _activationRoutine);
                _activationRoutine = null;
            }

            _isMoving = false;

            // Reset transform to start point
            if (_pathInitialised)
            {
                CalculateStartPosition();
            }
            else
            {
                // Defaults if not initialised yet
                _progress = 0;
                _nextPointIndex = 1;
                _isMovingForward = true;
            }
            
        }

        private void CheckMoveOnStart()
        {
            if (_moveOnStart)
            {
                Activate();
            }
        }

        #endregion

        #region Path follower

        public void SetPath(GameObject pathObject)
        {
            _pathParent = pathObject.transform;
            InitialisePath();
        }

        #endregion

        #region Triggers

        public void Trigger(string triggerKey)
        {
            switch(triggerKey)
            {
                case Globals.TRIGGER_MOVING:
                    TriggerMoving();
                    break;
            }
        }

        private void TriggerMoving()
        {
            if (!_isMoving && !_isActive && _pathInitialised && _activationRoutine == null)
            {
                _activationRoutine = StartCoroutine(Tools.delayThenAction(_activationDelay, () =>
                {
                    _isActive = true;
                    StartMoving();
                    _activationRoutine = null;
                }));
            }
        }

        #endregion

        #region Pathing

        private void InitialisePath()
        {
            int totalPoints = _pathParent.childCount;

            _pathSize = _isClosedPath ? totalPoints + 1 : totalPoints;

            if (_useDynamicPoints)
            {
                _pointTransforms = new Transform[_pathSize];
            }
            else
            {
                _points = new Vector3[_pathSize];
            }

            for (int i = 0; i < totalPoints; i++)
            {
                if (_useDynamicPoints)
                {
                    _pointTransforms[i] = _pathParent.GetChild(i);
                }
                else
                {
                    _points[i] = _pathParent.GetChild(i).position;
                }
                if (i > 0)
                {
                    _totalPathDistance += Vector3.Distance(GetPathPoint(i), GetPathPoint(i - 1));
                }
            }

            // Add the first position to the points array if this is a closed loop
            if (_isClosedPath)
            {
                if (_useDynamicPoints)
                {
                    _pointTransforms[totalPoints] = _pointTransforms[0];
                }
                else
                {
                    _points[totalPoints] = _points[0];
                }

                _totalPathDistance += Vector3.Distance(GetPathPoint(totalPoints - 1), GetPathPoint(0));
            }

            _totalPathTime = _totalPathDistance / _moveSpeed;

            // Start transform at first point
            //transform.position = GetPathPoint(0);

            CalculateStartPosition();

            _pathInitialised = true;
        }

        private void CalculateStartPosition()
        {
            float overallStartPos = _pathStartPosition * (_pathSize - 1);
            int startPointIndex = Mathf.FloorToInt(overallStartPos);
            float startProgress = overallStartPos - startPointIndex;

            if (_startDirection == PathDirection.Forward)
            {
                _isMovingForward = true;
                if (startPointIndex >= _pathSize - 1)
                {
                    transform.position = GetPathPoint(_pathSize-2);
                    _progress = 1;
                    _nextPointIndex = _pathSize-1;
                }
                else
                {
                    transform.position = GetPathPoint(startPointIndex);
                    _progress = startProgress;
                    _nextPointIndex = startPointIndex + 1;
                }
            }
            else
            {
                _isMovingForward = false;
                if (startPointIndex <= 0 && startProgress == 0)
                {
                    transform.position = GetPathPoint(1);
                    _progress = 1;
                    _nextPointIndex = 0;
                }
                else
                {
                    transform.position = GetPathPoint(startPointIndex + 1);
                    _progress = (1 - startProgress);
                    _nextPointIndex = startPointIndex;
                }
            }

        }

        private void FollowPath(float deltaTime)
        {
            if (_pathInitialised && _isMoving && _isActive)
            {
                _progress += _currentSpeed * deltaTime;

                if (_progress >= 1)
                {
                    transform.position = GetPathPoint(_nextPointIndex);

                    _isMoving = false;

                    if(_waitRoutine != null)
                    {
                        StopCoroutine(_waitRoutine);
                    }
                    _waitRoutine = StartCoroutine(Tools.delayThenAction(_pointWaitTime, () => { GetNextPathPoint(); _waitRoutine = null; }));
                }
                else
                {
                    float easedProgress = _easingFunction(0, 1, _progress);

                    if (_isMovingForward)
                    {
                        transform.position = Vector3.Lerp(GetPathPoint(_nextPointIndex - 1), GetPathPoint(_nextPointIndex), easedProgress);
                    }
                    else
                    {
                        transform.position = Vector3.Lerp(GetPathPoint(_nextPointIndex + 1), GetPathPoint(_nextPointIndex), easedProgress);
                    }
                }
            }
        }

        private void GetNextPathPoint()
        {
            if(_isMovingForward)
            {
                if(_nextPointIndex + 1 >= _pathSize)
                {
                    switch(_endOfPathInstruction)
                    {
                        case EndOfPathInstruction.Stop:
                            return;
                        case EndOfPathInstruction.Reverse:
                            _isMovingForward = false;
                            _nextPointIndex--;
                            break;
                        case EndOfPathInstruction.Loop:
                            _nextPointIndex = 1;
                            break;
                    }
                }
                else
                {
                    _nextPointIndex++;
                }
                _progress = 0;
                
                
            }
            else
            {
                if(_nextPointIndex - 1 < 0)
                {
                    switch(_endOfPathInstruction)
                    {
                        case EndOfPathInstruction.Stop:
                            return;
                        case EndOfPathInstruction.Reverse:
                            _isMovingForward = true;
                            _nextPointIndex++;
                            break;
                        case EndOfPathInstruction.Loop:
                            _nextPointIndex = _pathSize - 2;
                            break;
                    }
                }
                else
                {
                    _nextPointIndex--;
                }
                _progress = 0;

                
            }

            

            StartMoving();
        }

        private void StartMoving()
        {
            float nextDistance;
            Vector3 toNextPoint;
            if (_isMovingForward)
            {
                toNextPoint = GetPathPoint(_nextPointIndex) - GetPathPoint(_nextPointIndex - 1);

                nextDistance = toNextPoint.magnitude;
            }
            else
            {
                toNextPoint = GetPathPoint(_nextPointIndex) - GetPathPoint(_nextPointIndex + 1);

                nextDistance = toNextPoint.magnitude;
            }

            if (_rotateToFacePathDirection)
            {
                transform.forward = toNextPoint.normalized;
            }

            _isMoving = true;

            float distanceRatio = nextDistance / _totalPathDistance;
            float nextTime = _totalPathTime * distanceRatio;
            _currentSpeed = 1 / nextTime;

        }

        private Vector3 GetPathPoint(int index)
        {
            if(_useDynamicPoints)
            {
                return _pointTransforms[index].position;
            }
            else
            {
                return _points[index];
            }
        }

        #endregion

    }

    public enum PathDirection
    {
        Forward,
        Backward
    }
}