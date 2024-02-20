using AmalgamGames.Config;
using AmalgamGames.Control;
using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Simulation
{
    public class SimulatedRocketController : MonoBehaviour
    {
        // Props

        [SerializeField] private RocketConfig _config;

        private float _playerChargeForce;
        private float _minChargeForce;
        private float _engineBurnTime;
        private float _engineBurnForce;
        
        public float ChargeLevel { get { return _chargeLevel; } set { _chargeLevel = value; } }

        // EVENTS
        public event Action OnCollision;

        // STATE

        // Charging
        private float _chargeLevel = 0;
        private bool _isCharging = false;

        // Burning
        private bool _isBurning = false;
        private float _burnForce = 0;
        private float _burnLerp = 0;
        private float _burnDuration;

        // COROUTINES
        private Coroutine _engineBurnRoutine = null;

        // COMPONENTS
        private Rigidbody _rb;

        #region Lifecycle

        private void Start()
        {
            LoadConfig();

            _rb = GetComponent<Rigidbody>();

            // No gravity on level start, will reactivate on launch
            _rb.useGravity = false;
        }

        public void ManualFixedUpdate(float deltaTime)
        {
            if (_isBurning)
            {
                if (_burnLerp < _burnDuration)
                {
                    float burnForce = EasingFunction.EaseInCubic(_burnForce, 0, _burnLerp / _burnDuration);

                    _burnLerp += deltaTime;

                    transform.forward = _rb.velocity.normalized;
                    _rb.AddForce(transform.forward * burnForce * deltaTime, ForceMode.Force);
                }
                else
                {
                    FinishBurn();
                }
            }
        }

        #endregion

        #region Config

        private void LoadConfig()
        {
            _playerChargeForce = _config.PlayerChargeForce;
            _minChargeForce = _config.MinChargeForce;
            _engineBurnTime = _config.EngineBurnTime;
            _engineBurnForce = _config.EngineBurnForce;
        }

        #endregion

        #region Resetting

        private void SetVelocityToZero()
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        #endregion

        #region Charging

        public void Launch()
        {
            // Reactivate gravity if it was disabled
            _rb.useGravity = true;

            float engineBurnTime = _engineBurnTime * _chargeLevel;

            LaunchInfo launchInfo = new LaunchInfo(_chargeLevel, engineBurnTime);

            float launchStrength = _minChargeForce + (_chargeLevel * _playerChargeForce);

            SetVelocityToZero();

            // Launch
            _rb.AddForce(transform.forward * launchStrength, ForceMode.Impulse);

            // Disable charging for engine burn period
            if (_engineBurnRoutine != null)
            {
                Debug.LogError("Multiple burn routines active. This shouldn't happen");
                StopCoroutine(_engineBurnRoutine);
            }
            EngineBurn(launchInfo);

            _isCharging = false;
            _chargeLevel = 0;
        }

        private void FinishBurn()
        {
            _isBurning = false;

            _engineBurnRoutine = null;
        }

        private void EngineBurn(LaunchInfo launchInfo)
        {
            _burnLerp = 0;
            _burnForce = _engineBurnForce * launchInfo.ChargeLevel;
            _burnDuration = launchInfo.BurnDuration;

            _isBurning = true;
        }

        #endregion


        #region Collision

        private void OnCollisionEnter(Collision collision)
        {
            OnCollision?.Invoke();
        }

        #endregion
    }
}
