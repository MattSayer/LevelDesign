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
    public class ControllerRumble : ManagedBehaviour, IRespawnable, IPausable
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

        // State
        private bool _isSubscribedToCharging = false;
        private float _chargeLevel;

        // Coroutines
        private Coroutine _rumbleRoutine = null;

        // Components
        private IRocketController _rocketController;


        #region Lifecyle

        private void Start()
        {
            _rocketController = Tools.GetFirstComponentInHierarchy<IRocketController>(transform.parent);

            SubscribeToCharging();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToCharging();
            Gamepad.current?.ResumeHaptics();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromCharging();
            Gamepad.current?.PauseHaptics();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromCharging();
            Gamepad.current?.PauseHaptics();
        }

        public override void ManagedUpdate(float deltaTime)
        {
            if (_rumbleRoutine == null)
            {
                Gamepad.current?.SetMotorSpeeds(_chargeLevel * _maxLowFrequency, _chargeLevel * _maxHighFrequency);
            }
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
                if(_rumbleRoutine != null)
                {
                    StopCoroutine(_rumbleRoutine);
                }

                _chargeLevel = 0;

                _rumbleRoutine = StartCoroutine(collisionRumble());
            }
        }

        #endregion

        #region Charge events

        private void OnChargeLevelChanged(object rawValue)
        {
            if (rawValue.GetType() == typeof(float))
            {
                _chargeLevel = Mathf.Pow((float)rawValue, _chargeLevelPower);
            }
        }

        private void OnLaunch(LaunchInfo launchInfo)
        {
            if(_rumbleRoutine != null)
            {
                StopCoroutine(_rumbleRoutine);
            }

            _rumbleRoutine = StartCoroutine(launchRumble(launchInfo.BurnDuration));
        }

        #endregion

        #region Coroutines

        private IEnumerator launchRumble(float duration)
        {
            float time = 0;

            float highFrequency;
            float lowFrequency;
            EasingFunction.Function func = EasingFunction.GetEasingFunction(_launchRumbleEasing);

            while(time < duration)
            {
                float lerpVal = time / duration;
                highFrequency = func(_launchHighFrequency, 0, lerpVal);
                lowFrequency = func(_launchLowFrequency, 0, lerpVal);

                Gamepad.current?.SetMotorSpeeds(lowFrequency, highFrequency);

                time += Time.deltaTime;
                yield return null;
            }

            Gamepad.current?.SetMotorSpeeds(0, 0);

            _rumbleRoutine = null;

        }

        private IEnumerator collisionRumble()
        {
            float time = 0;

            float highFrequency;
            float lowFrequency;
            EasingFunction.Function func = EasingFunction.GetEasingFunction(_collisionRumbleEasing);

            while (time < _collisionRumbleDuration)
            {
                float lerpVal = time / _collisionRumbleDuration;
                highFrequency = func(_collisionHighFrequency, 0, lerpVal);
                lowFrequency = func(_collisionLowFrequency, 0, lerpVal);

                Gamepad.current?.SetMotorSpeeds(lowFrequency, highFrequency);

                time += Time.deltaTime;
                yield return null;
            }

            Gamepad.current?.SetMotorSpeeds(0, 0);

            _rumbleRoutine = null;
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