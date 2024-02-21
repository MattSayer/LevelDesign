using AmalgamGames.Effects;
using AmalgamGames.UpdateLoop;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmalgamGames.Utils
{
    public class ExternalTimeProvider : ManagedBehaviour, IExternalTimeProvider
    {
        [Title("Time settings")]
        [SerializeField] private bool _useUnscaledTime = false;
        [SerializeField] private float _timeMultiplier = 1;
        [Space]
        [Title("Dependency Provider")]
        [SerializeField] private DependencyRequest _getExternalTimeProvider;

        private List<Material> _timeMaterials = new List<Material>();

        // State
        private bool _isSubscribedToDependencyRequests = false;
        private float _currentTime = 0;

        // Statics
        private int _useExternalTimeHash;
        private int _externalTimeHash;

        #region Lifecycle

        private void Start()
        {
            _useExternalTimeHash = Shader.PropertyToID(Globals.USE_EXTERNAL_TIME_KEY);
            _externalTimeHash = Shader.PropertyToID(Globals.EXTERNAL_TIME_KEY);


            GetAllMaterials();
        }

        public override void ManagedUpdate(float deltaTime)
        {
            if(_useUnscaledTime)
            {
                deltaTime = Time.unscaledDeltaTime;
            }
            
            deltaTime *= _timeMultiplier;

            _currentTime += deltaTime;

            foreach(Material mat in _timeMaterials)
            {
                if (mat != null)
                {
                    mat.SetFloat(_externalTimeHash, _currentTime);
                }
            }
        }

        private void Awake()
        {
            SubscribeToDependencyRequests();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToDependencyRequests();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromDependencyRequests();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromDependencyRequests();
        }

        #endregion

        #region Dynamic materials

        public void RegisterExternalTimeMaterial(Material mat)
        {
            if (!_timeMaterials.Contains(mat))
            {
                _timeMaterials.Add(mat);
            }
        }

        public void UnregisterExternalTimeMaterial(Material mat)
        {
            if (_timeMaterials.Contains(mat))
            {
                _timeMaterials.Remove(mat);
            }
        }

        #endregion

        #region Initialisation

        private void GetAllMaterials()
        {
            // Get all materials
            // Iterate to find materials with UseExternalTime bool set to true

            GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (GameObject obj in allObjects)
            {
                if (obj.TryGetComponent(out Renderer renderer))
                {
                    if (renderer.material.HasProperty(_useExternalTimeHash))
                    {
                        _timeMaterials.Add(renderer.material);
                    }
                }
            }
        }

        #endregion

        #region Dependency requests

        private void ProvideDependency(Action<object> callback)
        {
            callback?.Invoke((IExternalTimeProvider)this);
        }

        private void SubscribeToDependencyRequests()
        {
            if (!_isSubscribedToDependencyRequests)
            {
                _getExternalTimeProvider.OnDependencyRequest += ProvideDependency;
                _isSubscribedToDependencyRequests = true;
            }
        }

        private void UnsubscribeFromDependencyRequests()
        {
            if (_isSubscribedToDependencyRequests)
            {
                _getExternalTimeProvider.OnDependencyRequest -= ProvideDependency;
                _isSubscribedToDependencyRequests = false;
            }
        }

        #endregion

    }

    public interface IExternalTimeProvider
    {
        public void RegisterExternalTimeMaterial(Material mat);

        public void UnregisterExternalTimeMaterial(Material mat);
    }
}
