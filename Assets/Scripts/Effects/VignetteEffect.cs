using AmalgamGames.Abilities;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using AmalgamGames.Core;
using System;

namespace AmalgamGames.Effects
{
    public class VignetteEffect : ToggleEffect, IRespawnable, IRestartable
    {
        [Title("Postprocessing")]
        [SerializeField] private Volume _postProcessingVolume;
        [Space]
        [Title("Settings")]
        [SerializeField] private float _vignetteIntensity = 0.5f;

        // STATE
        private float _defaultVignetteIntensity;

        // COMPONENTS
        private Vignette _vignette;

        #region Lifecycle

        protected override void Start()
        {
            base.Start();

            _postProcessingVolume.profile.TryGet<Vignette>(out _vignette);
            if (_vignette != null)
            {
                _defaultVignetteIntensity = _vignette.intensity.value;
            }
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

        protected override void ActivateEffect()
        {
            _vignette.intensity.value = _vignetteIntensity;
        }

        protected override void DeactivateEffect()
        {
            RemoveVignette();
        }

        #endregion
    }
}