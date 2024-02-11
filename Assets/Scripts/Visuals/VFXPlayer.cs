using AmalgamGames.Core;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Visuals
{
    public class VFXPlayer : MonoBehaviour, IVFXPlayer
    {
        [Title("Dependency Providers")]
        [SerializeField] private DependencyRequest _getVFXPlayer;


        #region Lifecycle

        private void Awake()
        {
            _getVFXPlayer.OnDependencyRequest += DependencyRequest;
        }

        #endregion

        #region VFX

        public void PlayVisualEffectAtLocation(string vfxID, Vector3 location)
        {

        }

        #endregion

        #region Dependency Requests

        private void DependencyRequest(Action<object> callback)
        {
            callback?.Invoke((IVFXPlayer)this);
        }

        #endregion

    }

    public interface IVFXPlayer
    {
        public void PlayVisualEffectAtLocation(string vfxID, Vector3 location);
    }
}