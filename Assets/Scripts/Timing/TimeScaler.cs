using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace AmalgamGames.Timing
{
    public class TimeScaler : MonoBehaviour, ITimeScaler
    {
        [Title("Dependency provider")]
        [SerializeField] private DependencyRequest _getTimeScaler;

        // Events
        public event Action<float> OnTimeScaleChanged;

        // Singleton
        private static TimeScaler _instance = null;
        
        // State
        private bool _isSubscribedToDependencyRequests = false;

        // Constants
        private float PHYSICS_TIMESTEP;

        #region Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            SubscribeToDependencyRequests();

            PHYSICS_TIMESTEP = Time.fixedDeltaTime;
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

        #region Time scaling

        public void SetTimeScale(float timeScale)
        {
            Time.timeScale = timeScale;
            Time.fixedDeltaTime = PHYSICS_TIMESTEP * timeScale;
            OnTimeScaleChanged?.Invoke(timeScale);
        }

        #endregion

        #region Dependency requests

        private void ProvideDependency(Action<object> callback)
        {
            callback?.Invoke((ITimeScaler)this);
        }

        private void SubscribeToDependencyRequests()
        {
            if (!_isSubscribedToDependencyRequests)
            {
                _getTimeScaler.OnDependencyRequest += ProvideDependency;
                _isSubscribedToDependencyRequests = true;
            }
        }

        private void UnsubscribeFromDependencyRequests()
        {
            if (_isSubscribedToDependencyRequests)
            {
                _getTimeScaler.OnDependencyRequest -= ProvideDependency;
                _isSubscribedToDependencyRequests = false;
            }
        }

        #endregion

    }

    public interface ITimeScaler
    {
        public event Action<float> OnTimeScaleChanged;
        public void SetTimeScale(float timeScale);
    }

}