using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmalgamGames.Utils;
using AmalgamGames.Effects;
using Sirenix.OdinInspector;
using UnityEngine;
using System;

namespace AmalgamGames.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class Respawner : MonoBehaviour
    {

        [Title("Respawning")]
        [SerializeField] private float _respawnDelay = 2;
        [Space]
        [Title("Components")]
        [SerializeField] private GameObject _meshObject;
        [SerializeField] private Transform _rocketTransform;
        [Space]
        [Title("DEBUG")]
        [SerializeField] private Transform _respawnPoint;

        // EVENTS
        public event Action<RespawnEvent> OnRespawnEvent;
        
        // COMPONENTS
        private IInputProcessor _inputProcessor;
        
        private List<IRespawnable> _respawnables;

        // STATE
        private bool _isSubscribedToInput = false;
        private bool _canCollide = true;
        private Transform _lastCheckpoint;
        private bool _isRespawning = false;

        // COROUTINES
        private Coroutine _explodeRoutine = null;

        #region Lifecycle

        private void Start()
        {
            _inputProcessor = Tools.GetFirstComponentInHierarchy<IInputProcessor>(_rocketTransform);
            SubscribeToInput();
            _lastCheckpoint = _respawnPoint;

            // Get all respawnables in the level
            _respawnables = new List<IRespawnable>();
            var respawnables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IRespawnable>();
            foreach(IRespawnable respawnable in respawnables)
            {
                _respawnables.Add(respawnable);
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInput();
        }

        #endregion

        #region Respawning

        private void OnRespawnInput()
        {
            if (!_isRespawning)
            {
                Respawn();
            }
        }

        private void Respawn()
        {
            foreach (IRespawnable respawnable in _respawnables)
            {
                respawnable.OnRespawnEvent(RespawnEvent.BeforeRespawn);
            }

            OnRespawnEvent?.Invoke(RespawnEvent.BeforeRespawn);

            // Fade screen down

            transform.position = _lastCheckpoint.position;
            transform.rotation = _lastCheckpoint.rotation;
            _canCollide = true;

            foreach (IRespawnable respawnable in _respawnables)
            {
                respawnable.OnRespawnEvent(RespawnEvent.OnRespawnStart);
            }

            OnRespawnEvent?.Invoke(RespawnEvent.OnRespawnStart);

            // Fade screen back up

            // Countdown

            foreach (IRespawnable respawnable in _respawnables)
            {
                respawnable.OnRespawnEvent(RespawnEvent.OnRespawnEnd);
            }

            OnRespawnEvent?.Invoke(RespawnEvent.OnRespawnEnd);

        }

        #endregion

        #region Input

        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnRespawn -= OnRespawnInput;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnRespawn += OnRespawnInput;
                _isSubscribedToInput = true;
            }
        }



        #endregion

        #region Collision

        private void OnCollisionEnter(Collision collision)
        {
            if (_canCollide)
            {
                if (collision.collider.CompareTag(Globals.FATAL_COLLIDER_TAG))
                {
                    if (_explodeRoutine == null)
                    {
                        _explodeRoutine = StartCoroutine(explodeThenRespawn());
                    }
                }
            }
            
        }

        private void Explode()
        {
            // Hide rocket mesh object
            _meshObject.SetActive(false);

            // Play explosion vfx

            Debug.Log("Boom!");


            _canCollide = false;
        }

        #endregion

        #region Coroutines

        private IEnumerator explodeThenRespawn()
        {
            _isRespawning = true;
            Explode();

            foreach (IRespawnable respawnable in _respawnables)
            {
                respawnable.OnRespawnEvent(RespawnEvent.OnCollision);
            }

            OnRespawnEvent?.Invoke(RespawnEvent.OnCollision);

            yield return new WaitForSeconds(_respawnDelay);

            // Re-enable mesh object
            _meshObject.SetActive(true);

            Respawn();

            _explodeRoutine = null;
            _isRespawning = false;
        }

        #endregion
    }
}