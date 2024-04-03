using AmalgamGames.Core;
using AmalgamGames.Editor;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AmalgamGames.Effects
{
    public class ParticleEffect : ToggleEffect
    {
        [Title("Components")]
        [SerializeField] private ParticleSystem _particleSystem;

        private bool _isInitiallyActive;

        #region Lifecyle

        private void Awake()
        {
            _isInitiallyActive = _particleSystem.main.playOnAwake;
        }

        #endregion

        #region Respawning/restarting

        public override void OnRespawnEvent(RespawnEvent evt)
        {
            base.OnRespawnEvent(evt);

            switch (evt)
            {
                case RespawnEvent.OnCollision:
                case RespawnEvent.OnRespawnStart:
                    StopParticleSystem();
                    ClearParticleSystem();
                    if(_isInitiallyActive)
                    {
                        StartParticleSystem();
                    }
                    break;
            }
        }

        private void StopParticleSystem()
        {
            _particleSystem.Stop();
        }

        private void ClearParticleSystem()
        {
            _particleSystem.Clear();
        }

        private void StartParticleSystem()
        {
            _particleSystem.Play();
        }

        #endregion

        #region Visuals

        protected override void ActivateEffect()
        {
            _particleSystem.Play();
        }

        protected override void DeactivateEffect()
        {
            StopParticleSystem();
        }

        #endregion

    }
}