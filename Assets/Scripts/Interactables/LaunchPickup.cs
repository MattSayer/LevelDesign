using AmalgamGames.Abilities;
using AmalgamGames.Control;
using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using UnityEngine;

namespace AmalgamGames.Interactables
{
    public class LaunchPickup : Interactable
    {

        [SerializeField] private float _launchForce = 100;

        // State
        private bool _isActive = false;
        private bool _isSubscribedToLaunch = false;

        // Components
        private Transform _playerRoot;
        private Slowmo _slowmo;
        private IRocketController _rocketController;
        private ICameraController _cameraController;
        private Rigidbody _rb;
        
        #region Pickup

        protected override void OnInteract(GameObject other)
        {
            _isActive = true;
            _playerRoot = other.transform;

            _rb = _playerRoot.GetComponent<Rigidbody>();

            ActivateSlowmo();

            EnableRocketLaunch();

            RemoveCameraSpeedLimit();
        }

        #endregion

        #region Respawning

        public override void OnRespawnEvent(RespawnEvent evt)
        {
            base.OnRespawnEvent(evt);
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    if(_isActive)
                    {
                        UnsubscribeFromLaunch();
                        EndSlowmo();
                        _isActive = false;
                    }
                    break;
            }
        }

        #endregion


        #region Launch

        private void ActivateSlowmo()
        {
            // Disable slowmo
            _slowmo = Tools.GetFirstComponentInHierarchy<Slowmo>(_playerRoot);
            if (_slowmo != default(Slowmo))
            {
                _slowmo.IgnoreInput(true);
                _slowmo.ForceActivateSlowmo();
            }
        }

        private void EnableRocketLaunch()
        {
            _rocketController = Tools.GetFirstComponentInHierarchy<IRocketController>(_playerRoot);
            if(_rocketController != default(IRocketController))
            {
                _rocketController.EnableImmediateLaunch();

                SubscribeToLaunch();
            }
        }

        private void OnLaunch(LaunchInfo launchInfo)
        {
            EndSlowmo();

            UnsubscribeFromLaunch();

            ApplyLaunchForce();

            _isActive = false;
        }

        private void RemoveCameraSpeedLimit()
        {
            _cameraController = GetCameraController(_playerRoot);
            if(_cameraController != default(ICameraController))
            {
                _cameraController.RemoveSpeedLimit(true);
            }
        }

        private void ApplyLaunchForce()
        {
            Vector3 launchForce = _launchForce * _playerRoot.forward;
            _rb.AddForce(launchForce, ForceMode.Impulse);
        }

        #endregion

        #region Helpers

        private void EndSlowmo()
        {
            _slowmo.CancelSlowmo();
            _slowmo.IgnoreInput(false);
        }

        private void SubscribeToLaunch()
        {
            _rocketController.OnLaunch += OnLaunch;
            _isSubscribedToLaunch = true;
        }

        private void UnsubscribeFromLaunch()
        {
            if (_isSubscribedToLaunch)
            {
                _rocketController.OnLaunch -= OnLaunch;
                _isSubscribedToLaunch = false;
            }
        }

        #endregion
    }
}