using Sirenix.OdinInspector;
using System;
using UnityEngine;
using AmalgamGames.Utils;
using AmalgamGames.Core;

namespace AmalgamGames.Effects
{
    public class ObjectToggler : ToggleEffect
    {
        [Title("Settings")]
        [SerializeField] private bool _startDisabled = false;
        [Space]
        [Title("Target")]
        [SerializeField] private GameObject[] _targetObjects;

        private bool _initiallyActive;

        #region Lifecycle

        private void Awake()
        {
            if(_targetObjects != null && _targetObjects.Length > 0)
            {
                _initiallyActive = _targetObjects[0].activeSelf;
            }
        }

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

        public override void OnRespawnEvent(RespawnEvent evt)
        {
            base.OnRespawnEvent(evt);
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    ResetEffect();
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

        private void ResetEffect()
        {
            bool toSetActive = _initiallyActive && !_startDisabled;
            foreach (GameObject obj in _targetObjects)
            {
                obj.SetActive(toSetActive);
            }
        }

        #endregion
    }
}