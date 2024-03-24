using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class ExternalForcesController : MonoBehaviour
    {
        // Components
        private Rigidbody _rb;

        // State
        private bool _isActive = true;

        #region Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        #endregion

        #region Forces

        public void ApplyExternalForce(Vector3 force, ForceMode forceMode)
        {
            if(_isActive)
            {
                _rb.AddForce(force, forceMode);
            }
        }

        public void DisableExternalForces()
        {
            if(_isActive)
            {
                _isActive = false;
            }
        }

        public void EnableExternalForces()
        {
            if(!_isActive)
            {
                _isActive = true;
            }
        }

        #endregion
    }
}