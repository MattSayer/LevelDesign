using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmalgamGames.Effects
{
    public class ControllerRumble : MonoBehaviour, IRespawnable, IPausable
    {


        // State
        private bool _isSubscribedToCharging = false;

        // Components
        private IRocketController _rocketController;


        #region Lifecyle

        private void Start()
        {
            _rocketController = Tools.GetFirstComponentInHierarchy<IRocketController>(transform.parent);

            SubscribeToCharging();
        }

        private void OnEnable()
        {
            SubscribeToCharging();
        }

        private void OnDisable()
        {
            UnsubscribeFromCharging();
        }

        private void OnDestroy()
        {
            UnsubscribeFromCharging();
        }

        #endregion

        #region Pausing

        public void Pause()
        {
            Gamepad.current?.PauseHaptics();
        }

        public void Resume()
        {
            Gamepad.current?.ResumeHaptics();
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            if(evt == RespawnEvent.OnCollision)
            {
                // Collision rumble
            }
        }

        #endregion

        #region Subscriptions

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
    }
}