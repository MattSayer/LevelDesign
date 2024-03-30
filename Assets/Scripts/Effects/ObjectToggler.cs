using Sirenix.OdinInspector;
using System;
using UnityEngine;
using AmalgamGames.Utils;
using AmalgamGames.Core;

namespace AmalgamGames.Effects
{
    public class ObjectToggler : ToggleEffect, IRespawnable
    {
        [Title("Settings")]
        [SerializeField] private bool _startDisabled = false;
        [Space]
        [Title("Target")]
        [SerializeField] private GameObject[] _targetObjects;
        
        #region Lifecycle

        protected override void Start()
        {
            base.Start();

            if (_startDisabled)
            {
                foreach(GameObject obj in _targetObjects)
                {
                    obj.SetActive(false);
                }
            }

        }

        protected override void OnDisable()
        {
            // Don't unsubscribe from events if the toggle target is this object, as doing so will
            // prevent activate events from firing and re-enabling this object
            foreach(GameObject obj in _targetObjects)
            {
                if (obj != gameObject)
                {
                    base.OnDisable();
                }
            }
            
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    DeactivateEffect();
                    break;
            }
        }

        #endregion

        #region Activating/Deactivating

        protected override void ActivateEffect()
        {
            foreach (GameObject obj in _targetObjects)
            {
                obj.SetActive(true);
            }
        }

        protected override void DeactivateEffect()
        {
            foreach (GameObject obj in _targetObjects)
            {
                obj.SetActive(false);
            }
        }

        #endregion
    }
}