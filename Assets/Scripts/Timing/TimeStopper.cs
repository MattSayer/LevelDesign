using AmalgamGames.Effects;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Timing
{
    public class TimeStopper : MonoBehaviour, ITimeStopper
    {
        [Title("Dependency provider")]
        [SerializeField] private DependencyRequest _getTimeStopper;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getTimeScaler;

        // State
        private float _cachedTimeScale = 1;
        private bool _isSubscribedToDependencyRequests = false;

        // Coroutines
        private Coroutine _stopTimeRoutine = null;

        // Components
        private ITimeScaler _timeScaler;

        #region Lifecycle

        private void Awake()
        {
            SubscribeToDependencyRequests();
        }

        private void Start()
        {
            _getTimeScaler.RequestDependency(ReceiveTimeScaler);
        }

        private void OnEnable()
        {
            SubscribeToDependencyRequests();
        }

        private void OnDisable()
        {
            UnsubscribeFromDependencyRequests();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDependencyRequests();
        }

        #endregion

        #region Timing

        public void StopTime(float duration)
        {
            if(_stopTimeRoutine != null)
            {
                Debug.LogError("Attempted to stop time while time already stopped. This shouldn't happen");
                return;
            }

            _stopTimeRoutine = StartCoroutine(stopTime(duration));
        }

        #endregion

        #region Coroutines

        private IEnumerator stopTime(float duration)
        {
            _cachedTimeScale = Time.timeScale;
            _timeScaler.SetTimeScale(0);
            yield return new WaitForSecondsRealtime(duration);
            _timeScaler.SetTimeScale(_cachedTimeScale);

            _stopTimeRoutine = null;
        }

        #endregion

        #region Dependency requests

        private void ProvideDependency(Action<object> callback)
        {
            callback?.Invoke((ITimeStopper)this);
        }

        private void SubscribeToDependencyRequests()
        {
            if (!_isSubscribedToDependencyRequests)
            {
                _getTimeStopper.OnDependencyRequest += ProvideDependency;
                _isSubscribedToDependencyRequests = true;
            }
        }

        private void UnsubscribeFromDependencyRequests()
        {
            if (_isSubscribedToDependencyRequests)
            {
                _getTimeStopper.OnDependencyRequest -= ProvideDependency;
                _isSubscribedToDependencyRequests = false;
            }
        }

        #endregion

        #region Dependencies

        private void ReceiveTimeScaler(object rawObj)
        {
            _timeScaler = rawObj as ITimeScaler;
        }

        #endregion
    }

    public interface ITimeStopper
    {
        public void StopTime(float duration);
    }
}
