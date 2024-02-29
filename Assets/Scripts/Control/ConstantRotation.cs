using AmalgamGames.UpdateLoop;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using AmalgamGames.Core;
using AmalgamGames.Utils;

namespace AmalgamGames.Control
{
    public class ConstantRotation : ManagedBehaviour, IRespawnable, ITriggerable
    {
        [Title("Trigger")]
        [SerializeField] private bool _rotateOnStart = true;
        [Title("Settings")]
        [Unit(Units.DegreesPerSecond)]
        [SerializeField] private float _rotationSpeed;
        [SerializeField] private TransformAxis _rotationAxis;
        [SerializeField] private Space _rotationSpace;

        // State
        private Quaternion _startRotation;
        private bool _isRotating = false;

        private Vector3 _worldRotationAxis;

        #region Lifecycle

        private void Start()
        {
            switch(_rotationSpace)
            {
                case Space.Self:
                    _worldRotationAxis = Tools.TranslateAxisInLocalSpace(transform, _rotationAxis);
                    break;
                case Space.World:
                    _worldRotationAxis = Tools.TranslateAxisInWorldSpace(_rotationAxis);
                    break;
            }

            _startRotation = transform.rotation;

            if(_rotateOnStart)
            {
                _isRotating = true;
            }
        }

        public override void ManagedUpdate(float deltaTime)
        {
            if(_isRotating)
            {
                Rotate(deltaTime);
            }
        }

        #endregion

        #region Triggers

        public void Trigger()
        {
            if(!_isRotating)
            {
                _isRotating = true;
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
                    if(_rotateOnStart)
                    {
                        _isRotating = true;
                    }
                    else
                    {
                        _isRotating = false;
                    }
                    break;
            }
        }

        #endregion

        #region Rotation

        private void Rotate(float deltaTime)
        {
            transform.RotateAround(transform.position, _worldRotationAxis, _rotationSpeed * deltaTime);
        }

        #endregion
    }
}