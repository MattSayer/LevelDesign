using Sirenix.OdinInspector;
using System;
using UnityEngine;
using AmalgamGames.Utils;

namespace AmalgamGames.Effects
{
    public abstract class ToggleEffect : MonoBehaviour
    {
        [FoldoutGroup("Events")] [SerializeField] private Component _eventSource;
        [FoldoutGroup("Events")] [SerializeField] private string _activateEventName;
        [FoldoutGroup("Events")] [SerializeField] private bool _activateEventHasParam = false;
        [FoldoutGroup("Events")] [SerializeField] private string _deactivateEventName;
        [FoldoutGroup("Events")] [SerializeField] private bool _deactivateEventHasParam = false;
        [Space]
        [FoldoutGroup("Settings")]
        [Tooltip("Delay activation of this effect after the target event fires")]
        [SerializeField] private float _activationDelay = 0;
        [FoldoutGroup("Settings")]
        [Tooltip("Delay deactivation of this effect after the target event fires")]
        [SerializeField] private float _deactivationDelay = 0;

        // STATE
        private bool _isSubscribedToEvents = false;

        private Delegate _activateHandler;
        private Delegate _deactivateHandler;

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
                object rawObj = (object)_eventSource;

                if (_activateEventName.Length > 0)
                {
                    if (_activateEventHasParam)
                    {
                        _activateHandler = Tools.WireUpEvent(rawObj, _activateEventName, this, nameof(DelayedActivateEffectWithParam));
                    }
                    else
                    {
                        _activateHandler = Tools.WireUpEvent(rawObj, _activateEventName, this, nameof(DelayedActivateEffect));
                    }
                }

                if (_deactivateEventName.Length > 0)
                {
                    if (_deactivateEventHasParam)
                    {
                        _deactivateHandler = Tools.WireUpEvent(rawObj, _deactivateEventName, this, nameof(DelayedDeactivateEffectWithParam));
                    }
                    else
                    {
                        _deactivateHandler = Tools.WireUpEvent(rawObj, _deactivateEventName, this, nameof(DelayedDeactivateEffect));
                    }
                }

                _isSubscribedToEvents = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_isSubscribedToEvents)
            {
                if (_activateEventName.Length > 0)
                {
                    Tools.DisconnectEvent((object)_eventSource, _activateEventName, _activateHandler);
                }
                if (_deactivateEventName.Length > 0)
                {
                    Tools.DisconnectEvent((object)_eventSource, _deactivateEventName, _deactivateHandler);
                }

                _isSubscribedToEvents = false;
            }
        }

        #endregion
    }
}