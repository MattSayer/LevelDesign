using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Core
{
    public class SharedFloatValue : MonoBehaviour
    {

        [Title("Settings")]
        [SerializeField] private float _initialValue;
        [SerializeField] private bool _useMinValue;
        [ShowIf("@this._useMinValue == true")]
        [SerializeField] private float _minValue;
        [SerializeField] private bool _useMaxValue;
        [ShowIf("@this._useMaxValue == true")]
        [SerializeField] private float _maxValue;

        private float _currentValue;

        public event Action<float> OnValueChanged;

        #region Lifecycle

        protected void Start()
        {
            _currentValue = _initialValue;
            OnValueChanged?.Invoke(_currentValue);
        }

        #endregion

        #region Public methods

        public bool AddValue(float valueToAdd)
        {
            if (valueToAdd < 0)
            {
                return false;
            }

            if ( _useMaxValue && (_initialValue + valueToAdd > _maxValue))
            {
                return false;
            }

            _currentValue += valueToAdd;

            return true;
        }

        public bool SubtractValue(float valueToSubtract)
        {
            if(valueToSubtract < 0)
            {
                return false;
            }

            if(_useMinValue && (_currentValue - valueToSubtract < _minValue))
            {
                return false;
            }

            _currentValue -= valueToSubtract;

            return true;
        }

        #endregion


    }
}