using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class MoveRBToTarget : MonoBehaviour
    {
        [Title("Components")]
        [SerializeField] private GameObject _targetToMove;
        [SerializeField] private Transform _sourceTransform;
        [Title("Trigger events")]
        [FoldoutGroup("Events")][SerializeField] private DynamicEvent[] _triggerEvents;

        // Components
        private Rigidbody _rb;

        // State
        private bool _isSubscribedToLaunch = false;

        #region Lifecycle

        private void Start()
        {
            _rb = _targetToMove.GetComponent<Rigidbody>();
            SubscribeToTrigger();
        }

        private void OnDisable()
        {
            UnsubscribeFromTrigger();
        }

        private void OnDestroy()
        {
            UnsubscribeFromTrigger();
        }

        private void OnEnable()
        {
            SubscribeToTrigger();
        }

        #endregion

        #region Trigger

        private void OnTriggerEvent(DynamicEvent sourceEvent, object param)
        {
            _rb.MovePosition(_sourceTransform.position);
        }

        #endregion

        #region Subscriptions

        private void SubscribeToTrigger()
        {
            if(!_isSubscribedToLaunch)
            {
                Tools.SubscribeToDynamicEvents(_triggerEvents, null, OnTriggerEvent);
                _isSubscribedToLaunch = true;
            }
        }

        private void UnsubscribeFromTrigger()
        {
            if (_isSubscribedToLaunch)
            {
                Tools.UnsubscribeFromDynamicEvents(_triggerEvents);
                _isSubscribedToLaunch = false;
            }
        }

        #endregion
    }
}