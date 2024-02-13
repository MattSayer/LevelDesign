using AmalgamGames.Abilities;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using AmalgamGames.Core;

namespace AmalgamGames.Effects
{
    public class SlowmoVisuals : MonoBehaviour, IRespawnable, IRestartable
    {
        [Title("Components")]
        [SerializeField] private Slowmo _slowmo;
        [Space]
        [Title("Postprocessing")]
        [SerializeField] private Volume _postProcessingVolume;
        [Space]
        [Title("Settings")]
        [SerializeField] private float _slowmoVignetteIntensity = 0.5f;

        // STATE
        private bool _isSubscribedToSlowmo = false;
        private float _defaultVignetteIntensity;

        // COMPONENTS
        private Vignette _vignette;

        #region Lifecycle
        
        private void Start()
        {
            SubscribeToSlowmo();
            _postProcessingVolume.profile.TryGet<Vignette>(out _vignette);
            if(_vignette != null)
            {
                _defaultVignetteIntensity = _vignette.intensity.value;
            }
        }

        private void OnEnable()
        {
            SubscribeToSlowmo();
        }

        private void OnDisable()
        {
            UnsubscribeFromSlowmo();   
        }

        private void OnDestroy()
        {
            UnsubscribeFromSlowmo();
        }

        #endregion

        #region Respawning/restarting

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnCollision:
                case RespawnEvent.OnRespawnStart:
                    RemoveVignette();
                    break;
            }
            
        }
        
        private void RemoveVignette()
        {
            _vignette.intensity.value = _defaultVignetteIntensity;
        }

        public void DoRestart()
        {
            RemoveVignette();
        }

        #endregion

        #region Visuals

        private void OnSlowmoStart()
        {
            _vignette.intensity.value = _slowmoVignetteIntensity;
        }

        private void OnSlowmoEnd()
        {
            RemoveVignette();
        }

        #endregion

        #region Subscriptions

        private void SubscribeToSlowmo()
        {
            if(!_isSubscribedToSlowmo && _slowmo != null)
            {
                _slowmo.OnSlowmoStart += OnSlowmoStart;
                _slowmo.OnSlowmoEnd += OnSlowmoEnd;
                _isSubscribedToSlowmo = true;
            }
        }

        private void UnsubscribeFromSlowmo()
        {
            if (_isSubscribedToSlowmo && _slowmo != null)
            {
                _slowmo.OnSlowmoStart -= OnSlowmoStart;
                _slowmo.OnSlowmoEnd -= OnSlowmoEnd;
                _isSubscribedToSlowmo = false;
            }
        }

        #endregion
    }
}