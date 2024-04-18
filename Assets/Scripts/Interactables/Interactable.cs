using AmalgamGames.Audio;
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
        [SerializeField] private string _interactAudioClipID;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getAudioManager;
        [Space]
        [Title("Settings")]
        [SerializeField] private bool _canOnlyInteractOnce = false;
        [SerializeField] protected bool _deactivateOnInteract = false;
        [SerializeField] private GameObject _targetForActivation;

        // Components
        private IAudioManager _audioManager;

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

        private void Start()
        {
            _getAudioManager?.RequestDependency(ReceiveAudioManager);
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

                    if (!string.IsNullOrEmpty(_interactAudioClipID))
                    {
                        AudioPlayRequest audioRequest = new AudioPlayRequest { audioType = Audio.AudioType.Flat, audioClipID = _interactAudioClipID };
                        _audioManager?.PlayAudioClip(audioRequest);
                    }

                    OnInteract(rb.gameObject);
                    if (_deactivateOnInteract)
                    {
                        _targetForActivation.SetActive(false);
                    }
                }
            }
        }

        #endregion

        #region Dependency requests

        private void ReceiveAudioManager(object rawObj)
        {
            _audioManager = rawObj as AudioManager;
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