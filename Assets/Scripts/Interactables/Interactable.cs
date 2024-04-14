using AmalgamGames.Control;
using AmalgamGames.Core;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Interactables
{
    public abstract class Interactable : MonoBehaviour, IRespawnable
    {
        [Title("Audio")]
        [SerializeField] private AudioClip _interactSound;

        [Title("Settings")]
        [SerializeField] private bool _canOnlyInteractOnce = false;
        [SerializeField] protected bool _deactivateOnInteract = false;
        [SerializeField] private GameObject _targetForActivation;

        // State
        protected bool _hasBeenInteracted = false;
        protected bool _bankedHasBeenInteracted = false;

        #region Lifecycle

        protected virtual void Awake()
        {
            if(_targetForActivation == null)
            {
                _targetForActivation = gameObject;
            }
        }

        #endregion

        #region Respawning

        public virtual void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    _hasBeenInteracted = _bankedHasBeenInteracted;
                    if (_hasBeenInteracted && _deactivateOnInteract)
                    {
                        _targetForActivation.SetActive(false);
                    }
                    else
                    {
                        _targetForActivation.SetActive(true);
                    }
                    break;
                case RespawnEvent.OnCheckpoint:
                    _bankedHasBeenInteracted = _hasBeenInteracted;
                    break;
            }
        }

        #endregion

        #region Abstract methods

        protected abstract void OnInteract(GameObject other);

        #endregion

        #region Collision

        private void OnTriggerEnter(Collider other)
        {
            if(!_hasBeenInteracted || !_canOnlyInteractOnce)
            {
                // Gets closest parent rigidbody, to handle compound colliders
                Rigidbody rb = Tools.GetClosestParentComponent<Rigidbody>(other.transform);
                if(rb != default(Rigidbody))
                {
                    _hasBeenInteracted = true;
                    OnInteract(rb.gameObject);
                    if (_deactivateOnInteract)
                    {
                        _targetForActivation.SetActive(false);
                    }
                }
            }
        }

        #endregion

        #region Helpers

        protected ICameraController GetCameraController(Transform playerRoot)
        {
            return Tools.GetFirstComponentInHierarchy<ICameraController>(playerRoot.parent);
        }

        protected IRocketController GetRocketController(Transform playerRoot)
        {
            return Tools.GetFirstComponentInHierarchy<IRocketController>(playerRoot);
        }

        #endregion
    }
}