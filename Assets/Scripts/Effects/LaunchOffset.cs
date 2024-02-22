using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cinemachine.CinemachineOrbitalTransposer;

namespace AmalgamGames.Effects
{
    public class LaunchOffset : MonoBehaviour
    {
        [Title("Components")]
        [SerializeField] private GameObject _rocketObject;
        [SerializeField] private Transform _camTarget;

        // Components
        private IRocketController _rocketController;
        private Rigidbody _rocketRB;

        // State
        private bool _isSubscribedToLaunch = false;

        #region Lifecycle

        private void Start()
        {
            _rocketController = _rocketObject.GetComponent<IRocketController>();
            _rocketRB = _rocketObject.GetComponent<Rigidbody>();
            SubscribeToLaunch();
        }

        private void OnDisable()
        {
            UnsubscribeFromLaunch();
        }

        private void OnDestroy()
        {
            UnsubscribeFromLaunch();
        }

        private void OnEnable()
        {
            SubscribeToLaunch();
        }

        #endregion

        #region Launch

        private void OnLaunch(LaunchInfo launchInfo)
        {
            _rocketRB.MovePosition(_camTarget.position);

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