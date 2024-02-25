using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using PathCreation;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace AmalgamGames.Control
{
    public class TransformPathFollower : ManagedBehaviour, IRespawnable
    {
        [Title("Path")]
        [SerializeField] private Transform _pathParent;
        [SerializeField] private EndOfPathInstruction _endOfPathInstruction;
        [SerializeField] private float _pointWaitTime = 0;
        [SerializeField] private bool _isClosedPath = false;
        [SerializeField] private bool _useDynamicPoints = false;

        [Space]
        
        [Title("Movement")]
        [Unit(Units.MetersPerSecond)]
        [SerializeField] private float _moveSpeed;
        [SerializeField] private bool _moveOnStart = true;
        [SerializeField] private EasingFunction.Ease _easingType = EasingFunction.Ease.Linear;

        [Space]
        
        [Title("Rotation")]
        [SerializeField] private bool _rotateToFacePathDirection = false;

        // State
        private int _nextPointIndex = 1;
        private float _progress = 0;
        private bool _isMoving = false;
        private bool _isMovingForward = true;
        private float _currentSpeed;

        // Path
        private EasingFunction.Function _easingFunction;
        private float _totalPathDistance;
        private float _totalPathTime;
        private Transform[] _pointTransforms;
        private Vector3[] _points;
        private int _pathSize;
        


        #region Lifecycle

        private void Start()
        {
            _easingFunction = EasingFunction.GetEasingFunction(_easingType);

            InitialisePath();

            if(_moveOnStart)
            {
                _isMoving = true;
            }
        }

        public override void ManagedUpdate(float deltaTime)
        {
            FollowPath(deltaTime);
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch (evt)
            {
                case RespawnEvent.OnRespawnStart:
                    ResetPathProgress();
                    break;
            }
        }

        private void ResetPathProgress()
        {
            _progress = 0;
            _nextPointIndex = 1;
            _isMovingForward = true;

            CalculateCurrentSpeed();
        }

        #endregion

        #region Pathing

        private void InitialisePath()
        {
            int totalPoints = _pathParent.childCount;

            _pathSize = _isClosedPath ? totalPoints + 1: totalPoints;

            if (_useDynamicPoints)
            {
                _pointTransforms = new Transform[_pathSize];
            }
            else
            {
                _points = new Vector3[_pathSize];
            }

            for(int i = 0 ; i < totalPoints; i++)
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
                    _totalPathDistance += Vector3.Distance(GetPathPoint(i), GetPathPoint(i-1));
                }
            }

            // Add the first position to the points array if this is a closed loop
            if(_isClosedPath)
            {
                if(_useDynamicPoints)
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

            CalculateCurrentSpeed();
        }

        private void FollowPath(float deltaTime)
        {
            if (_isMoving)
            {
                _progress += _currentSpeed * deltaTime;

                if (_progress >= 1)
                {
                    transform.position = GetPathPoint(_nextPointIndex);
                    GetNextPathPoint();
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

            

            CalculateCurrentSpeed();
        }

        private void CalculateCurrentSpeed()
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
}