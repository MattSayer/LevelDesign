using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using PathCreation;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AmalgamGames.Control
{
    public class BezierPathsFollower : ManagedFixedBehaviour
    {
        [Title("Paths")]
        [SerializeField] private BezierPathSection[] _pathSections;
        [Space]
        [Title("Pathing")]
        [SerializeField] private EndOfPathInstruction _endOfPathsInstruction;
        [SerializeField] private float _endOfPathsDelay = 0;
        [Space]
        [Title("Movement")]
        [SerializeField] private bool _moveOnStart = true;
        [SerializeField] private float _activationDelay = 0;

        // Components
        private EasingFunction.Function _easingFunction;

        // Coroutines
        private Coroutine _activationRoutine = null;
        private Coroutine _endSectionRoutine = null;

        // Path State
        private int _currentSectionIndex = 0;
        private VertexPath _currentPath;
        private float _currentSectionDuration = 0;
        private float _currentSectionEndWaitTime = 0;

        // General state
        private bool _isSectionIncreasing = true;
        private bool _isActive = false;
        private bool _isMoving = false;

        private float _sectionTime;


        #region Lifecycle

        private void Awake()
        {
            if (_pathSections.Length > 0)
            {
                InitialisePath();
            }
        }

        public override void ManagedFixedUpdate(float deltaTime)
        {
            FollowPath(deltaTime);
        }

        #endregion


        #region Initialisation

        private void InitialisePath()
        {
            if (_pathSections.Length > 0 && _moveOnStart && _activationRoutine == null)
            {
                _activationRoutine = StartCoroutine(Tools.delayThenAction(_activationDelay, () =>
                {
                    _isActive = true;
                    InitialisePathSection(0);
                    _activationRoutine = null;                    
                }));
            }
        }

       

        private void InitialisePathSection(int index)
        {
            BezierPathSection section = _pathSections[index];
            _currentPath = section.Path.path;
            _currentSectionIndex = index;
            
            _easingFunction = EasingFunction.GetEasingFunction(section.EasingFunction);
            _currentSectionDuration = section.Duration;
            _currentSectionEndWaitTime = section.EndSectionWaitTime;

            // Reset section time
            _sectionTime = 0;

            _isMoving = true;
        }

        #endregion

        #region Following
        private void FollowPath(float deltaTime)
        {
            if (_isActive && _isMoving)
            {
                float lerpVal = _sectionTime / _currentSectionDuration;

                if (lerpVal >= 1)
                {
                    if (_endSectionRoutine == null)
                    {
                        _endSectionRoutine = StartCoroutine(endCurrentSection());
                    }
                }
                else
                {
                    float easedProgress = _isSectionIncreasing ? _easingFunction(0, 1, lerpVal) : _easingFunction(1, 0, lerpVal);
                    transform.position = _currentPath.GetPointAtTime(easedProgress, EndOfPathInstruction.Stop);

                    _sectionTime += deltaTime;
                }
                
            }
        }

        #endregion

        #region Switching sections

        private IEnumerator endCurrentSection()
        {
            _isMoving = false;
            if(_currentSectionEndWaitTime > 0)
            {
                yield return new WaitForSeconds(_currentSectionEndWaitTime);
            }

            if(_isSectionIncreasing)
            {
                if(_currentSectionIndex >= _pathSections.Length - 1)
                {
                    if(_endOfPathsDelay > 0)
                    {
                        yield return new WaitForSeconds(_endOfPathsDelay);
                    }

                    switch(_endOfPathsInstruction)
                    {
                        case EndOfPathInstruction.Stop:
                            _isActive = false;
                            break;
                        case EndOfPathInstruction.Reverse:
                            _isSectionIncreasing = false;
                            InitialisePathSection(_currentSectionIndex);
                            break;
                        case EndOfPathInstruction.Loop:
                            InitialisePathSection(0);
                            break;
                    }
                }
                else
                {
                    _currentSectionIndex++;
                    InitialisePathSection(_currentSectionIndex);
                    _isMoving = true;
                }
            }
            else
            {
                if(_currentSectionIndex <= 0)
                {
                    if (_endOfPathsDelay > 0)
                    {
                        yield return new WaitForSeconds(_endOfPathsDelay);
                    }

                    switch (_endOfPathsInstruction)
                    {
                        case EndOfPathInstruction.Stop:
                            _isActive = false;
                            break;
                        case EndOfPathInstruction.Reverse:
                            _isSectionIncreasing = true;
                            InitialisePathSection(0);
                            break;
                        case EndOfPathInstruction.Loop:
                            InitialisePathSection(_pathSections.Length - 1);
                            break;
                    }
                }
                else
                {
                    _currentSectionIndex--;
                    InitialisePathSection(_currentSectionIndex);
                    _isMoving = true;
                }
            }

            _endSectionRoutine = null;
        }

        #endregion
    }

    [System.Serializable]
    public class BezierPathSection
    {
        public PathCreator Path;
        public float Duration;
        public EasingFunction.Ease EasingFunction = Utils.EasingFunction.Ease.Linear;
        public float EndSectionWaitTime = 0;
    }
}