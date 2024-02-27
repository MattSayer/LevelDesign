using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Core
{
    public class TriggerProxy : MonoBehaviour
    {
        public event Action<Collider> OnProxyTriggerEnter;
        public event Action<Collider> OnProxyTriggerExit;

        #region Triggers

        private void OnTriggerEnter(Collider other)
        {
            OnProxyTriggerEnter?.Invoke(other);
        }

        private void OnTriggerExit(Collider other)
        {
            OnProxyTriggerExit?.Invoke(other);
        }

        #endregion
    }
}