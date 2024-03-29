using Sirenix.OdinInspector;
using System;
using UnityEngine;
using System.Collections.Generic;
using AmalgamGames.Utils;
using AmalgamGames.Conditionals;

namespace AmalgamGames.Effects
{
    public abstract class ToggleEffect : MonoBehaviour
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

        #region Effect

        protected abstract void ActivateEffect();

        protected abstract void DeactivateEffect();

        protected void DelayedActivateEffect()
        {
            Invoke(nameof(ActivateEffect),_activationDelay);
        }

        protected void DelayedActivateEffectWithParam(DynamicEvent sourceEvent, object param)
        {
            foreach(ConditionalCheck conditional in sourceEvent.Conditionals)
            {
                if(!conditional.ApplyCheck(param))
                {
                    return;
                }
            }
            DelayedActivateEffect();
        }

        protected void DelayedDeactivateEffect()
        {
            Invoke(nameof(DeactivateEffect),_deactivationDelay);
        }

        protected void DelayedDeactivateEffectWithParam(DynamicEvent sourceEvent, object param)
        {
            foreach (ConditionalCheck conditional in sourceEvent.Conditionals)
            {
                if (!conditional.ApplyCheck(param))
                {
                    return;
                }
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