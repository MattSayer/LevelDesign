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
        [FoldoutGroup("Events")] [SerializeField] private DynamicEventsWithDelay[] _activateEvents;
        [FoldoutGroup("Events")] [SerializeField] private DynamicEventsWithDelay[] _deactivateEvents;

        // STATE
        private bool _isSubscribedToEvents = false;

        // Coroutines
        private Coroutine _effectRoutine = null;

        #region Lifecycle

        protected virtual void Awake()
        {
            SubscribeToEvents();
        }

        protected virtual void Start()
        {
            
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

        protected void DelayedActivateEffect(float delay)
        {
            if(_effectRoutine != null)
            {
                StopCoroutine(_effectRoutine);
            }

            if (delay > 0)
            {
                _effectRoutine = StartCoroutine(Tools.delayThenAction(delay, () =>
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

        protected void DelayedActivateEffectWithParam(DynamicEvent sourceEvent, object param, float delay)
        {
            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if(!conditionalCheck)
            {
                return;
            }
            DelayedActivateEffect(delay);
        }

        protected void DelayedDeactivateEffect(float delay)
        {
            if (_effectRoutine != null)
            {
                StopCoroutine(_effectRoutine);
            }

            if (delay > 0)
            {
                _effectRoutine = StartCoroutine(Tools.delayThenAction(delay, () =>
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

        protected void DelayedDeactivateEffectWithParam(DynamicEvent sourceEvent, object param, float delay)
        {
            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if(!conditionalCheck)
            {
                return;
            }
            DelayedDeactivateEffect(delay);
        }

        #endregion

        #region Subscriptions

        private void SubscribeToEvents()
        {
            if (!_isSubscribedToEvents)
            {
                foreach(DynamicEventsWithDelay evt in _activateEvents)
                {
                    Tools.SubscribeToDynamicEvents(evt.DynamicEvents, () => { DelayedActivateEffect(evt.Delay); }, (dynEvent, param) => { DelayedActivateEffectWithParam(dynEvent, param, evt.Delay); });
                }
                
                foreach(DynamicEventsWithDelay evt in _deactivateEvents)
                {
                    Tools.SubscribeToDynamicEvents(evt.DynamicEvents, () => { DelayedDeactivateEffect(evt.Delay); }, (dynEvent, param) => { DelayedDeactivateEffectWithParam(dynEvent, param, evt.Delay); });
                }

                _isSubscribedToEvents = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_isSubscribedToEvents)
            {
                foreach (DynamicEventsWithDelay evt in _activateEvents)
                {
                    Tools.UnsubscribeFromDynamicEvents(evt.DynamicEvents);
                }

                foreach (DynamicEventsWithDelay evt in _deactivateEvents)
                {
                    Tools.UnsubscribeFromDynamicEvents(evt.DynamicEvents);
                }

                _isSubscribedToEvents = false;
            }
        }

        #endregion
    }

    [Serializable]
    public class DynamicEventsWithDelay
    {
        public DynamicEvent[] DynamicEvents;
        public float Delay = 0;
    }
}