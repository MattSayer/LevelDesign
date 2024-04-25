using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmalgamGames.Utils;
using AmalgamGames.Effects;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using AmalgamGames.Timing;

namespace AmalgamGames.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class Respawner : MonoBehaviour, IValueProvider, ILevelStateListener
    {

        [Title("Respawning")]
        [SerializeField] private float _respawnDelay = 2;
        [Space]
        [Title("Components")]
        [SerializeField] private GameObject _meshObject;
        [SerializeField] private TriggerProxy _checkpointTrigger;
        [SerializeField] private Countdown _countdown;
        [Space]
        [Title("Spawning")]
        [SerializeField] private Transform _initialSpawnPoint;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getInputProcessor;

        // EVENTS
        public event Action<RespawnEventInfo> OnRespawnEvent;
        private event Action<object> OnNumRespawnsChanged;
        
        // COMPONENTS
        private IInputProcessor _inputProcessor;
        
        private List<IRespawnable> _respawnables;

        // Subscriptions
        private bool _isSubscribedToInput = false;
        private bool _isSubscribedToTrigger = false;

        // State
        private bool _canCollide = true;
        private bool _isRespawning = false;

        // Level state
        private bool _hasLevelStarted = false;

        // Tracking
        private Transform _lastCheckpoint;
        private int _numRespawns = 0;

        // COROUTINES
        private Coroutine _explodeRoutine = null;

        #region Lifecycle

        private void Start()
        {
            // Get all respawnables in the level
            _respawnables = new List<IRespawnable>();
            var respawnables = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IRespawnable>();
            foreach(IRespawnable respawnable in respawnables)
            {
                _respawnables.Add(respawnable);
            }
        }

        private void OnEnable()
        {
            if(_hasLevelStarted)
            {
                SubscribeToInput();

                SubscribeToTrigger();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
            UnsubscribeFromTrigger();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInput();
            UnsubscribeFromTrigger();
        }

        #endregion

        #region Level state

        public void OnLevelStateChanged(LevelState levelState)
        {
            switch(levelState)
            {
                case LevelState.Started:
                    StartLevel();
                    break;
            }
        }

        private void StartLevel()
        {
            _getInputProcessor.RequestDependency(ReceiveInputProcessor);
            
            SubscribeToTrigger();
            _lastCheckpoint = _initialSpawnPoint;

            //Respawn(true);
            
            StartCountdown();
            
            _hasLevelStarted = true;
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
            _isRespawning = true;
            
            _numRespawns++;
            OnNumRespawnsChanged?.Invoke(_numRespawns);

            foreach (IRespawnable respawnable in _respawnables)
            {
                respawnable.OnRespawnEvent(RespawnEvent.BeforeRespawn);
            }

            OnRespawnEvent?.Invoke(new RespawnEventInfo(RespawnEvent.BeforeRespawn));

            // Fade screen down

            transform.position = _lastCheckpoint.position;
            transform.rotation = _lastCheckpoint.rotation;
            _canCollide = true;

            foreach (IRespawnable respawnable in _respawnables)
            {
                respawnable.OnRespawnEvent(RespawnEvent.OnRespawnStart);
            }

            OnRespawnEvent?.Invoke(new RespawnEventInfo(RespawnEvent.OnRespawnStart));

            // Fade screen back up
            FinishRespawning();
        }
        

        private void FinishRespawning()
        {
            foreach (IRespawnable respawnable in _respawnables)
            {
                respawnable.OnRespawnEvent(RespawnEvent.OnRespawnEnd);
            }

            OnRespawnEvent?.Invoke(new RespawnEventInfo(RespawnEvent.OnRespawnEnd));

            _isRespawning = false;
        }

        #endregion

        #region Countdown
        
        private void StartCountdown()
        {
            // Countdown

                _countdown.OnCountdownFinished += OnCountdownFinished;
                _countdown.StartCountdown(Globals.COUNTDOWN_DURATION);
        }

        private void OnCountdownFinished()
        {
            _countdown.OnCountdownFinished -= OnCountdownFinished;

            foreach(IRespawnable respawnable in _respawnables)
            {
                respawnable.OnRespawnEvent(RespawnEvent.OnInitialSpawnEnded);
            }
            
            OnRespawnEvent?.Invoke(new RespawnEventInfo(RespawnEvent.OnInitialSpawnEnded));
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

        private void SubscribeToTrigger()
        {
            if(!_isSubscribedToTrigger && _checkpointTrigger != null)
            {
                _checkpointTrigger.OnProxyTriggerEnter += OnProxyTriggerEnter;
                _isSubscribedToTrigger = true;
            }
        }

        private void UnsubscribeFromTrigger()
        {
            if (_isSubscribedToTrigger && _checkpointTrigger != null)
            {
                _checkpointTrigger.OnProxyTriggerEnter -= OnProxyTriggerEnter;
                _isSubscribedToTrigger = false;
            }
        }

        #endregion

        #region Triggers

        private void OnProxyTriggerEnter(Collider other)
        {
            // Check for checkpoint triggers
            if (other.gameObject.layer == Globals.CHECKPOINT_LAYER)
            {
                // Notify respawnables that a new checkpoint has been reached
                foreach(IRespawnable respawnable in _respawnables)
                {
                    respawnable.OnRespawnEvent(RespawnEvent.OnCheckpoint);
                }

                _lastCheckpoint = other.transform;
            }
        }

        #endregion

        #region Collision

        private void OnTriggerEnter(Collider collider)
        {
            if (_canCollide)
            {
                if (collider.CompareTag(Globals.FATAL_COLLIDER_TAG))
                {
                    if (_explodeRoutine == null)
                    {
                        _explodeRoutine = StartCoroutine(explodeThenRespawn());
                    }
                }
            }
        }

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

        private void OnParticleCollision(GameObject other)
        {
            if (_canCollide)
            {
                if (other.CompareTag(Globals.FATAL_COLLIDER_TAG))
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

            _canCollide = false;
        }

        #endregion

        #region Value Provider

        public void SubscribeToValue(string valueName, Action<object> callback)
        {
            switch(valueName)
            {
                case Globals.NUM_RESPAWNS_KEY:
                    OnNumRespawnsChanged += callback;
                    break;
            }
        }

        public void UnsubscribeFromValue(string valueName, Action<object> callback)
        {
            switch (valueName)
            {
                case Globals.NUM_RESPAWNS_KEY:
                    OnNumRespawnsChanged -= callback;
                    break;
            }
        }

        #endregion

        #region Dependencies

        private void ReceiveInputProcessor(object rawObj)
        {
            _inputProcessor = rawObj as IInputProcessor;
            SubscribeToInput();
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

            OnRespawnEvent?.Invoke(new RespawnEventInfo(RespawnEvent.OnCollision));

            yield return new WaitForSeconds(_respawnDelay);

            // Re-enable mesh object
            _meshObject.SetActive(true);

            Respawn();

            _explodeRoutine = null;
        }


        #endregion
    }
}