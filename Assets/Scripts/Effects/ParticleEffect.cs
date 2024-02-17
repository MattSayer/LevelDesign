using AmalgamGames.Core;
using AmalgamGames.Editor;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AmalgamGames.Effects
{
    public class ParticleEffect : ToggleEffect, IRespawnable
    {
        [Title("Components")]
        [SerializeField] private ParticleSystem _particleSystem;

        #region Respawning/restarting

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch (evt)
            {
                case RespawnEvent.OnCollision:
                case RespawnEvent.OnRespawnStart:
                    StopParticleSystem();
                    break;
            }
        }

        private void StopParticleSystem()
        {
            _particleSystem.Stop();
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