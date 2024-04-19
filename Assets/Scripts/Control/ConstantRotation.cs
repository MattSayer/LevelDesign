using AmalgamGames.UpdateLoop;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using AmalgamGames.Core;
using AmalgamGames.Utils;

namespace AmalgamGames.Control
{
    public class ConstantRotation : ManagedFixedBehaviour, IRespawnable, ITriggerable
    {
        [Title("Trigger")]
        [SerializeField] private bool _rotateOnStart = true;
        [SerializeField] private float _startDelay = 0;
        [Title("Settings")]
        [Unit(Units.DegreesPerSecond)]
        [SerializeField] private float _rotationSpeed;
        [SerializeField] private TransformAxis _rotationAxis;
        [SerializeField] private Space _rotationSpace;
        

        // State
        private Quaternion _startRotation;
        private bool _isRotating = false;

        private Vector3 _worldRotationAxis;

        // Coroutines
        private Coroutine _startRoutine = null;

        #region Lifecycle

        private void Awake()
        {
            _startRotation = transform.rotation;

            switch (_rotationSpace)
            {
                case Space.Self:
                    _worldRotationAxis = Tools.TranslateAxisInLocalSpace(transform, _rotationAxis);
                    break;
                case Space.World:
                    _worldRotationAxis = Tools.TranslateAxisInWorldSpace(_rotationAxis);
                    break;
            }
        }

        private void Start()
        {
            if(_rotateOnStart)
            {
                StartRotating();
            }
        }

        public override void ManagedFixedUpdate(float deltaTime)
        {
            if(_isRotating)
            {
                Rotate(deltaTime);
            }
        }

        #endregion

        #region Triggers

        public void Trigger(string triggerKey)
        {
            switch(triggerKey)
            {
                case Globals.TRIGGER_ROTATION:
                    StartRotating();
                    break;
            }
            
        }


        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    transform.rotation = _startRotation;
                    
                    if(_startRoutine != null)
                    {
                        StopCoroutine(_startRoutine);
                    }

                    _isRotating = false;

                    if (_rotateOnStart)
                    {
                        StartRotating();
                    }
                    break;
            }
        }

        #endregion

        #region Rotation

        private void StartRotating()
        {
            if (!_isRotating)
            {
                if (_startDelay > 0)
                {
                    _startRoutine = StartCoroutine(Tools.delayThenAction(_startDelay, () =>
                    {
                        _isRotating = true;
                        _startRoutine = null;
                    }));
                }
                else
                {
                    _isRotating = true;
                }
            }
        }

        private void Rotate(float deltaTime)
        {
            transform.RotateAround(transform.position, _worldRotationAxis, _rotationSpeed * deltaTime);
        }

        #endregion
    }
}