using Sirenix.OdinInspector;
using System;
using UnityEngine;
using System.Collections.Generic;
using AmalgamGames.Utils;

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

        protected void DelayedActivateEffectWithParam(object param)
        {
            DelayedActivateEffect();
        }

        protected void DelayedDeactivateEffect()
        {
            Invoke(nameof(DeactivateEffect),_deactivationDelay);
        }

        protected void DelayedDeactivateEffectWithParam(object param)
        {
            DelayedDeactivateEffect();
        }

        #endregion

        #region Subscriptions

        private void SubscribeToEvents()
        {
            if (!_isSubscribedToEvents)
            {
                foreach(DynamicEvent activateEvent in _activateEvents)
                {
                    object rawObj = (object)activateEvent.EventSource;

                    Delegate activateHandler;

                    if (activateEvent.EventHasParam)
                    {
                        activateHandler = Tools.WireUpEvent(rawObj, activateEvent.EventName, this, nameof(DelayedActivateEffectWithParam));
                    }
                    else
                    {
                        activateHandler = Tools.WireUpEvent(rawObj, activateEvent.EventName, this, nameof(DelayedActivateEffect));
                    }
                    activateEvent.EventHandler = activateHandler;
                }

                foreach (DynamicEvent deactivateEvent in _deactivateEvents)
                {
                    object rawObj = (object)deactivateEvent.EventSource;

                    Delegate deactivateHandler;

                    if (deactivateEvent.EventHasParam)
                    {
                        deactivateHandler = Tools.WireUpEvent(rawObj, deactivateEvent.EventName, this, nameof(DelayedDeactivateEffectWithParam));
                    }
                    else
                    {
                        deactivateHandler = Tools.WireUpEvent(rawObj, deactivateEvent.EventName, this, nameof(DelayedDeactivateEffect));
                    }
                    deactivateEvent.EventHandler = deactivateHandler;
                }

                _isSubscribedToEvents = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_isSubscribedToEvents)
            {
                foreach (DynamicEvent activateEvent in _activateEvents)
                {
                    Tools.DisconnectEvent((object)activateEvent.EventSource, activateEvent.EventName, activateEvent.EventHandler);
                }

                foreach (DynamicEvent deactivateEvent in _deactivateEvents)
                {
                    Tools.DisconnectEvent((object)deactivateEvent.EventSource, deactivateEvent.EventName, deactivateEvent.EventHandler);
                }

                _isSubscribedToEvents = false;
            }
        }

        #endregion
    }

    [Serializable]
    public class DynamicEvent
    {
        public Component EventSource;
        public string EventName;
        public bool EventHasParam = false;
        public Delegate EventHandler;
    }
}