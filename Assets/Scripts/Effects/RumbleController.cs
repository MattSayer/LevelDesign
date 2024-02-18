using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmalgamGames.Effects
{
    public class RumbleController : ManagedBehaviour, IRumbleController, IPausable, IRespawnable
    {
        [Title("Dependency Provider")]
        [SerializeField] private DependencyRequest _getRumbleController;

        // Intensity buffers
        private float _lowIntensityBuffer;
        private float _highIntensityBuffer;

        // Continuous rumble tracking
        private Dictionary<MonoBehaviour, RumbleIntensity> _rumbleRequests = new Dictionary<MonoBehaviour, RumbleIntensity>();

        private bool _isSubscribedToDependencyRequests = false;

        #region Lifecycle

        private void Awake()
        {
            SubscribeToDependencyRequests();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToDependencyRequests();
            Gamepad.current?.ResumeHaptics();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromDependencyRequests();
            Gamepad.current?.PauseHaptics();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromDependencyRequests();
            Gamepad.current?.PauseHaptics();
        }

        public override void ManagedUpdate(float deltaTime)
        {
            CalculateContinuousIntensity();
            Gamepad.current?.SetMotorSpeeds(_lowIntensityBuffer, _highIntensityBuffer);
            ClearIntensityBuffers();
        }

        #endregion

        #region Rumble requests

        public void RumbleBurst(RumbleIntensity intensity, float duration, EasingFunction.Ease falloffEasing = EasingFunction.Ease.Linear)
        {
            StartCoroutine(doRumbleBurst(intensity, duration, falloffEasing));
        }

        public void ContinuousRumble(MonoBehaviour instigator, RumbleIntensity intensity)
        {
            _rumbleRequests[instigator] = intensity;
        }

        public void StopContinuousRumble(MonoBehaviour instigator)
        {
            if (_rumbleRequests.ContainsKey(instigator))
            { 
                _rumbleRequests.Remove(instigator);
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
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    StopAllCoroutines();
                    _rumbleRequests.Clear();
                    ClearIntensityBuffers();
                    break;
            }
        }

        #endregion

        #region Intensity handling

        private void AddIntensity(RumbleIntensity intensity)
        {
            // Rumble isn't additive, so take max of current and new value
            _lowIntensityBuffer = Mathf.Max(_lowIntensityBuffer, intensity.LowIntensity);
            _highIntensityBuffer = Mathf.Max(_highIntensityBuffer, intensity.HighIntensity);
        }

        private void CalculateContinuousIntensity()
        {
            foreach(RumbleIntensity intensity in _rumbleRequests.Values)
            {
                _lowIntensityBuffer = Mathf.Max(_lowIntensityBuffer, intensity.LowIntensity);
                _highIntensityBuffer = Mathf.Max(_highIntensityBuffer, intensity.HighIntensity);
            }
        }

        private void ClearIntensityBuffers()
        {
            _lowIntensityBuffer = 0;
            _highIntensityBuffer = 0;
        }

        #endregion

        #region Coroutines

        private IEnumerator doRumbleBurst(RumbleIntensity intensity, float duration, EasingFunction.Ease falloffEasing)
        {
            float time = 0;
            EasingFunction.Function easingFunction = EasingFunction.GetEasingFunction(falloffEasing);
            while(time < duration)
            {
                float lerpVal = time / duration;
                float lowVal = easingFunction(intensity.LowIntensity, 0, lerpVal);
                float highVal = easingFunction(intensity.HighIntensity, 0, lerpVal);
                AddIntensity(new RumbleIntensity(lowVal, highVal));

                time += Time.deltaTime;
                yield return null;
            }
        }

        #endregion

        #region Dependency requests

        private void ProvideDependency(Action<object> callback)
        {
            callback?.Invoke((IRumbleController)this);
        }

        private void SubscribeToDependencyRequests()
        {
            if (!_isSubscribedToDependencyRequests)
            {
                _getRumbleController.OnDependencyRequest += ProvideDependency;
                _isSubscribedToDependencyRequests = true;
            }
        }

        private void UnsubscribeFromDependencyRequests()
        {
            if(_isSubscribedToDependencyRequests)
            {
                _getRumbleController.OnDependencyRequest -= ProvideDependency;
                _isSubscribedToDependencyRequests = false;
            }
        }

        #endregion
    }

    public struct RumbleIntensity
    {
        public float LowIntensity;
        public float HighIntensity;

        public RumbleIntensity(float lowIntensity, float highIntensity)
        {
            LowIntensity = lowIntensity;
            HighIntensity = highIntensity;
        }
    }

    public interface IRumbleController
    {
        public void RumbleBurst(RumbleIntensity intensity, float duration, EasingFunction.Ease falloffEasing = EasingFunction.Ease.Linear);

        public void ContinuousRumble(MonoBehaviour instigator, RumbleIntensity intensity);
        
        public void StopContinuousRumble(MonoBehaviour instigator);

    }
}