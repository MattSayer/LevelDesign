using Sirenix.OdinInspector;
using System;
using UnityEngine;
using AmalgamGames.Utils;

namespace AmalgamGames.Effects
{
    public class ObjectToggler : ToggleEffect
    {
        [Title("Settings")]
        [SerializeField] private bool _startDisabled = false;
        [Space]
        [Title("Target")]
        [SerializeField] private GameObject _targetObj;
        
        #region Lifecycle

        protected override void Start()
        {
            base.Start();

            if (_startDisabled)
            {
                _targetObj.SetActive(false);
            }

        }

        #endregion

        #region Activating/Deactivating

        protected override void ActivateEffect()
        {
            _targetObj.SetActive(true);
        }

        protected override void DeactivateEffect()
        {
            _targetObj.SetActive(false);
        }

        #endregion
    }
}