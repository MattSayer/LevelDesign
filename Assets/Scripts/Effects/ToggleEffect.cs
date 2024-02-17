using Sirenix.OdinInspector;
using System;
using UnityEngine;
using AmalgamGames.Utils;

namespace AmalgamGames.Effects
{
    public abstract class ToggleEffect : MonoBehaviour
    {
        [Title("Event")]
        [SerializeField] private Component _eventSource;
        [SerializeField] private string _activateEventName;
        [SerializeField] private string _deactivateEventName;
        [Space]
        [Title("Settings")]
        [Tooltip("Delay activation of this effect after the target event fires")]
        [SerializeField] private float _activationDelay = 0;
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

        protected void DelayedDeactivateEffect()
        {
            Invoke(nameof(DeactivateEffect),_deactivationDelay);
        }


        #endregion

        #region Subscriptions

        private void SubscribeToEvents()
        {
            if (!_isSubscribedToEvents)
            {
                object rawObj = (object)_eventSource;

                _activateHandler = Tools.WireUpEvent(rawObj, _activateEventName, this, nameof(DelayedActivateEffect));

                _deactivateHandler = Tools.WireUpEvent(rawObj, _deactivateEventName, this, nameof(DelayedDeactivateEffect));

                _isSubscribedToEvents = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_isSubscribedToEvents)
            {
                Tools.DisconnectEvent((object)_eventSource, _activateEventName, _activateHandler);
                Tools.DisconnectEvent((object)_eventSource, _deactivateEventName, _deactivateHandler);

                _isSubscribedToEvents = false;
            }
        }

        #endregion
    }
}