using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cinemachine.CinemachineOrbitalTransposer;

namespace AmalgamGames.Effects
{
    public class LaunchOffset : ManagedFixedBehaviour
    {
        [Title("Components")]
        [SerializeField] private GameObject _rocketObject;
        [SerializeField] private Transform _camTarget;
        [Space]
        [Title("Settings")]
        [SerializeField] private float _pushForce = 10f;

        // Components
        private IRocketController _rocketController;
        private Rigidbody _rocketRB;

        // State
        private bool _isSubscribedToLaunch = false;

        private Vector3 _launchPosition = Vector3.zero;
        private float _pushDistance = 0;
        private Vector3 _pushDirection = Vector3.zero;
        private Vector3 _originalHeading = Vector3.zero;


        #region Lifecycle

        private void Start()
        {
            _rocketController = _rocketObject.GetComponent<IRocketController>();
            _rocketRB = _rocketObject.GetComponent<Rigidbody>();
            SubscribeToLaunch();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromLaunch();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromLaunch();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToLaunch();
        }

        public override void ManagedFixedUpdate(float deltaTime)
        {
            if(_pushDirection != Vector3.zero)
            {
                _rocketRB.AddForce(_pushDirection * deltaTime * _pushForce, ForceMode.Force);

                // Once rocket is aligned with the original camera direction, stop pushing
                
                Vector3 fromLaunch = _rocketRB.position - _launchPosition;

                float pushDistFromLaunch = Vector3.Dot(fromLaunch, _pushDirection);

                pushDistFromLaunch *= fromLaunch.magnitude;

                if (pushDistFromLaunch >= _pushDistance)
                {
                    float velocityInHeading = Vector3.Dot(_rocketRB.velocity.normalized, _originalHeading);

                    Vector3 newVelocity = _originalHeading * velocityInHeading * _rocketRB.velocity.magnitude;

                    _rocketRB.velocity = newVelocity;

                    _pushDirection = Vector3.zero;
                    _originalHeading = Vector3.zero;
                }
            }
        }


        #endregion

        #region Launch

        private void OnLaunch(LaunchInfo launchInfo)
        {
            _originalHeading = _rocketObject.transform.forward;

            Vector3 delta = _rocketObject.transform.position - _camTarget.position;

            // Push rocket towards cam target
            _pushDirection = delta.normalized;
            _pushDistance = delta.magnitude;
            _launchPosition = _rocketObject.transform.position;
            

        }

        #endregion

        #region Subscriptions

        private void SubscribeToLaunch()
        {
            if(!_isSubscribedToLaunch && _rocketController != null)
            {
                _rocketController.OnLaunch += OnLaunch;
                _isSubscribedToLaunch = true;
            }
        }

        private void UnsubscribeFromLaunch()
        {
            if (_isSubscribedToLaunch && _rocketController != null)
            {
                _rocketController.OnLaunch -= OnLaunch;
                _isSubscribedToLaunch = false;
            }
        }

        #endregion
    }
}