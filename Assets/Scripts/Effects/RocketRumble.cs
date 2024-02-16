using AmalgamGames.Core;
using AmalgamGames.UI;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmalgamGames.Effects
{
    public class RocketRumble : MonoBehaviour, IRespawnable, IPausable
    {
        [Title("Charging")]
        [SerializeField] private float _maxLowFrequency;
        [SerializeField] private float _maxHighFrequency;
        [SerializeField] private float _chargeLevelPower = 2;
        [Space]
        [Title("Launch")]
        [SerializeField] private float _launchLowFrequency;
        [SerializeField] private float _launchHighFrequency;
        [SerializeField] private EasingFunction.Ease _launchRumbleEasing = EasingFunction.Ease.Linear;
        [Space]
        [Title("Collision")]
        [SerializeField] private float _collisionLowFrequency;
        [SerializeField] private float _collisionHighFrequency;
        [SerializeField] private float _collisionRumbleDuration = 1;
        [SerializeField] private EasingFunction.Ease _collisionRumbleEasing = EasingFunction.Ease.Linear;
        [Space]
        [Title("Components")]
        [SerializeField] private Transform _rocketTransform;
        [Space]
        [Title("Dependency Requests")]
        [SerializeField] private DependencyRequest _getRumbleController;

        // State
        private bool _isSubscribedToCharging = false;

        // Components
        private IRocketController _rocketController;
        private IRumbleController _rumbleController;


        #region Lifecyle

        private void Start()
        {
            _rocketController = Tools.GetFirstComponentInHierarchy<IRocketController>(_rocketTransform);

            _getRumbleController.RequestDependency(ReceiveRumbleController);

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
                _rumbleController.RumbleBurst(new RumbleIntensity(_collisionLowFrequency, _collisionHighFrequency),_collisionRumbleDuration,_collisionRumbleEasing);
            }
        }

        #endregion

        #region Charge events

        private void OnChargeLevelChanged(object rawValue)
        {
            if (rawValue.GetType() == typeof(float))
            {
                float chargeLevel = Mathf.Pow((float)rawValue, _chargeLevelPower);
                _rumbleController.ContinuousRumble(gameObject, new RumbleIntensity(chargeLevel * _maxLowFrequency,chargeLevel * _maxHighFrequency));
            }
        }

        private void OnLaunch(LaunchInfo launchInfo)
        {
            _rumbleController.RumbleBurst(new RumbleIntensity(_launchLowFrequency,_launchHighFrequency),launchInfo.BurnDuration,_launchRumbleEasing);
        }

        #endregion

        #region Dependency Requests

        private void ReceiveRumbleController(object rawObj)
        {
            _rumbleController = rawObj as RumbleController;
        }

        #endregion

        #region Subscriptions

        private void SubscribeToCharging()
        {
            if (!_isSubscribedToCharging && _rocketController != null)
            {
                ((IValueProvider)_rocketController).SubscribeToValue(Globals.CHARGE_LEVEL_CHANGED_KEY, OnChargeLevelChanged);
                _rocketController.OnLaunch += OnLaunch;
                _isSubscribedToCharging = true;
            }
        }

        private void UnsubscribeFromCharging()
        {
            if (_isSubscribedToCharging && _rocketController != null)
            {
                ((IValueProvider)_rocketController).UnsubscribeFromValue(Globals.CHARGE_LEVEL_CHANGED_KEY, OnChargeLevelChanged);
                _rocketController.OnLaunch -= OnLaunch;
                _isSubscribedToCharging = false;
            }
        }

        #endregion
    }
}