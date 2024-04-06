using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public abstract class DynamicEventsEffect : MonoBehaviour
    {
        protected abstract DynamicEventsContainer[] DynamicEventsContainers { get;}

        // State
        private bool _isSubscribedToEvents = false;

        #region Lifecycle

        protected virtual void Awake()
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

        #region Trigger events

        protected abstract void OnTriggerEvent(DynamicEventsContainer sourceEvent);

        /// <summary>
        /// Event that gets called when the source trigger event fires. Any conditionals have already been applied
        /// to the parameter, so they do not need to be checked again
        /// </summary>
        /// <param name="sourceTransformationEvent"></param>
        /// <param name="sourceEvent"></param>
        /// <param name="param"></param>
        protected abstract void OnTriggerEventWithParam(DynamicEventsContainer sourceTransformationEvent, DynamicEvent sourceEvent, object param);

        private void TriggerEventWithParam(DynamicEventsContainer sourceTransformationEvent, DynamicEvent sourceEvent, object param)
        {
            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if (!conditionalCheck)
            {
                return;
            }

            OnTriggerEventWithParam(sourceTransformationEvent, sourceEvent, param);
        }
        
        #endregion

        #region Subscriptions

        private void SubscribeToEvents()
        {
            if (!_isSubscribedToEvents)
            {
                foreach (DynamicEventsContainer evt in DynamicEventsContainers)
                {
                    Tools.SubscribeToDynamicEvents(evt.DynamicEvents, () => OnTriggerEvent(evt), (DynamicEvent dynEvent, object param) => TriggerEventWithParam(evt, dynEvent, param));
                }

                _isSubscribedToEvents = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_isSubscribedToEvents)
            {
                foreach (DynamicEventsContainer evt in DynamicEventsContainers)
                {
                    Tools.UnsubscribeFromDynamicEvents(evt.DynamicEvents);
                }

                _isSubscribedToEvents = false;
            }
        }

        #endregion
    }
}