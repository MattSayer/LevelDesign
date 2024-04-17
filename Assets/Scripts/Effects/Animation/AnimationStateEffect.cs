using AmalgamGames.Core;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{ 
    public class AnimationStateEffect : DynamicEventsEffect, IRespawnable
    {
        [FoldoutGroup("Events")]
        [SerializeField] private DynamicEventsWithAnimationState[] _animationEvents;
        [Space]
        [FoldoutGroup("Animators")]
        [SerializeField] private Animator[] _targetAnimators;
        [Space]
        [Title("Settings")]
        [SerializeField] private string _defaultStateName;

        protected override DynamicEventsContainer[] DynamicEventsContainers => _animationEvents;


        private Dictionary<string, int> _animationHashes;

        // Coroutines
        private Coroutine _lerpRoutine = null;

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();
            
            // Set default property value to initial value of first material
            // Default property value is used when respawning

            // Pre hash all the possible state names from the triggerable animations
            _animationHashes = new Dictionary<string, int>();

            foreach(DynamicEventsWithAnimationState animEvent in _animationEvents)
            {
                string stateName = animEvent.StateName;
                _animationHashes[stateName] = Animator.StringToHash(stateName);
            }

            _animationHashes[_defaultStateName] = Animator.StringToHash(_defaultStateName);
        }

        #endregion

        #region Trigger events

        protected override void OnTriggerEvent(DynamicEventsContainer sourceEvent)
        {
            DynamicEventsWithAnimationState evt = (DynamicEventsWithAnimationState)sourceEvent;

            int animHash = _animationHashes[evt.StateName];

            foreach(Animator animator in _targetAnimators)
            {
                animator.Play(animHash);
            }
        }

        protected override void OnTriggerEventWithParam(DynamicEventsContainer sourceTransformationEvent, DynamicEvent sourceEvent, object param)
        {
            DynamicEventsWithAnimationState evt = (DynamicEventsWithAnimationState)sourceTransformationEvent;

            int animHash = evt.UseEventParameter && param.GetType() == typeof(string) ? Animator.StringToHash(param.ToString()) : _animationHashes[evt.StateName];

            foreach (Animator animator in _targetAnimators)
            {
                animator.Play(animHash);
            }
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch (evt)
            {
                case RespawnEvent.OnRespawnStart:
                    KillAllCoroutines();
                    ResetAnimatorToDefaultState();
                    break;
            }
        }

        private void ResetAnimatorToDefaultState()
        {
            foreach (Animator animator in _targetAnimators)
            {
                animator.Play(_animationHashes[_defaultStateName],0,1);
            }
        }

        private void KillAllCoroutines()
        {
            if (_lerpRoutine != null)
            {
                StopCoroutine(_lerpRoutine);
            }
        }

        #endregion

        [Serializable]
        private class DynamicEventsWithAnimationState : DynamicEventsContainer
        {
            public string StateName;
        }
    }
}