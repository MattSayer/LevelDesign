using Sirenix.OdinInspector;
using System;
using UnityEngine;
using System.Collections.Generic;
using AmalgamGames.Utils;
using AmalgamGames.Conditionals;
using Technie.PhysicsCreator;
using AmalgamGames.Core;

namespace AmalgamGames.Effects
{
    public abstract class ToggleEffect : MonoBehaviour, IRespawnable
    {
        [FoldoutGroup("Events")] [SerializeField] private DynamicEvent[] _activateEvents;
        [FoldoutGroup("Events")] [SerializeField] private DynamicEvent[] _deactivateEvents;
        [Space]
        [FoldoutGroup("Settings")]
        [Tooltip("Delay activation of this effect after the target event fires")]
        [SerializeField] private float _activationDelay = 0;
        [FoldoutGroup("Settings")]
        [Tooltip("Delay deactivation of this effect after the target event fires")]
        [SerializeField] private float _deactivationDelay = 0;

        // STATE
        private bool _isSubscribedToEvents = false;

        // Coroutines
        private Coroutine _effectRoutine = null;

        private List<Delegate> _activateHandlers;
        private List<Delegate> _deactivateHandlers;

        #region Lifecycle

        protected virtual void Start()
        {
            SubscribeToEvents();
        }

        protected virtual void OnEnable()
        {
            SubscribeToEvents();
        }

        protected virtual void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        #endregion

        #region Respawning

        public virtual void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    KillAllCoroutines();
                    break;
            }
        }

        private void KillAllCoroutines()
        {
            if(_effectRoutine != null)
            {
                StopCoroutine(_effectRoutine);
                _effectRoutine = null;
            }
        }

        #endregion

        #region Effect

        protected abstract void ActivateEffect();

        protected abstract void DeactivateEffect();

        protected void DelayedActivateEffect()
        {
            if(_effectRoutine != null)
            {
                StopCoroutine(_effectRoutine);
            }

            if (_activationDelay > 0)
            {
                _effectRoutine = StartCoroutine(Tools.delayThenAction(_activationDelay, () =>
                {
                    ActivateEffect();
                    _effectRoutine = null;
                }));
            }
            else
            {
                ActivateEffect();
            }
        }

        protected void DelayedActivateEffectWithParam(DynamicEvent sourceEvent, object param)
        {
            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if(!conditionalCheck)
            {
                return;
            }
            DelayedActivateEffect();
        }

        protected void DelayedDeactivateEffect()
        {
            if (_effectRoutine != null)
            {
                StopCoroutine(_effectRoutine);
            }

            if (_deactivationDelay > 0)
            {
                _effectRoutine = StartCoroutine(Tools.delayThenAction(_deactivationDelay, () =>
                {
                    DeactivateEffect();
                    _effectRoutine = null;
                }));
            }
            else
            {
                DeactivateEffect();
            }
        }

        protected void DelayedDeactivateEffectWithParam(DynamicEvent sourceEvent, object param)
        {
            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if(!conditionalCheck)
            {
                return;
            }
            DelayedDeactivateEffect();
        }

        #endregion

        #region Subscriptions

        private void SubscribeToEvents()
        {
            if (!_isSubscribedToEvents)
            {
                Tools.SubscribeToDynamicEvents(_activateEvents, DelayedActivateEffect, DelayedActivateEffectWithParam);

                Tools.SubscribeToDynamicEvents(_deactivateEvents, DelayedDeactivateEffect, DelayedDeactivateEffectWithParam);

                _isSubscribedToEvents = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_isSubscribedToEvents)
            {
                Tools.UnsubscribeFromDynamicEvents(_activateEvents);

                Tools.UnsubscribeFromDynamicEvents(_deactivateEvents);

                _isSubscribedToEvents = false;
            }
        }

        #endregion
    }
}