using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Core
{
    public class ObjectToggler : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField] private bool _startDisabled = false;
        [Space]
        [Title("Target")]
        [SerializeField] private GameObject _targetObj;
        [Space]
        [Title("Events")]
        [SerializeField] private Component _eventSource;
        [SerializeField] private string _activateEventName;
        [SerializeField] private string _deactivateEventName;

        private Delegate _activateHandler;
        private Delegate _deactivateHandler;

        #region Lifecycle

        private void Start()
        {
            object rawObj = (object)_eventSource;

            _activateHandler = Tools.WireUpEvent(rawObj, _activateEventName, this, nameof(ActivateObject));

            _deactivateHandler = Tools.WireUpEvent(rawObj, _deactivateEventName, this, nameof(DeactivateObject));

            if (_startDisabled)
            {
                _targetObj.SetActive(false);
            }

        }

        private void OnDestroy()
        {
            Tools.DisconnectEvent((object)_eventSource, _activateEventName, _activateHandler);
            Tools.DisconnectEvent((object)_eventSource, _deactivateEventName, _deactivateHandler);
        }

        #endregion

        #region Activating/Deactivating

        public void ActivateObject()
        {
            _targetObj.SetActive(true);
        }

        public void DeactivateObject()
        {
            _targetObj.SetActive(false);
        }

        #endregion
    }
}