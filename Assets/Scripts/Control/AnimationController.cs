using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AmalgamGames.Utils;
using AmalgamGames.Core;

namespace AmalgamGames.Control
{
    public class AnimationController : MonoBehaviour, ITriggerable, IRespawnable
    {

        [Title("Settings")]
        [SerializeField] private bool _playOnStart = false;
        [SerializeField] private float _startDelay = 0;
        [SerializeField] private float _playbackSpeed = 1.0f;
        [Title("Components")]
        [SerializeField] private Animator _animator;


        // State
        private bool _isPlaying = false;

        // Hashes
        private int START_TRIGGER_HASH;
        private int STOP_TRIGGER_HASH;

        // Coroutines
        private Coroutine _startRoutine = null;

        #region Lifecycle

        private void Start()
        {
            START_TRIGGER_HASH = Animator.StringToHash(Globals.ANIMATION_START_TRIGGER);
            STOP_TRIGGER_HASH = Animator.StringToHash(Globals.ANIMATION_STOP_TRIGGER);

            _animator.speed = _playbackSpeed;

            if (_playOnStart && _animator != null)
            {
                StartAnimating();
            }

        }

        #endregion

        #region Animation

        private void StartAnimating()
        {
            if (_startRoutine == null)
            {
                if (_startDelay > 0)
                {
                    _startRoutine = StartCoroutine(Tools.delayThenAction(_startDelay, () =>
                    {
                        _isPlaying = true;
                        _animator?.ResetTrigger(STOP_TRIGGER_HASH);
                        _animator?.SetTrigger(START_TRIGGER_HASH);
                        _startRoutine = null;
                    }));
                }
                else
                {
                    _isPlaying = true;
                    _animator?.ResetTrigger(STOP_TRIGGER_HASH);
                    _animator?.SetTrigger(START_TRIGGER_HASH);
                }
            }
        }

        private void StopAnimating()
        {
            if (_startRoutine != null)
            {
                StopCoroutine(_startRoutine);
                _startRoutine = null;
            }

            _isPlaying = false;
            _animator?.ResetTrigger(START_TRIGGER_HASH);
            _animator?.SetTrigger(STOP_TRIGGER_HASH);
        }

        #endregion

        #region Triggers

        public void Trigger()
        {
            if(!_isPlaying)
            {
                StartAnimating();
            }
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    StopAnimating();
                    if(_playOnStart)
                    {
                        StartAnimating();
                    }
                    break;
            }
        }

        #endregion
    }
}