using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace AmalgamGames.Core
{
    public class SharedIntValue: MonoBehaviour, IRestartable, IValueProvider
    {

        [Title("Settings")]
        [SerializeField] private string _valueKey;
        [SerializeField] private int _initialValue;
        [SerializeField] private bool _initialiseOnStart = true;
        [Space]
        [Title("Minimum value")]
        [SerializeField] private bool _useMinValue;
        [ShowIf("@this._useMinValue == true")]
        [SerializeField] private int _minValue;
        [ShowIf("@this._useMinValue == true")]
        [Tooltip("Allow operations that would decrease value below min value by clamping to min value")]
        [SerializeField] private bool _allowOverflowOperationsAndClampMin = false;
        [ShowIf("@this._useMinValue == true")]
        [Tooltip("The maximum amount a Subtract operation can go below the specified minimum value. Set to < 0 to allow unlimited overflow")]
        [SerializeField] private int _overflowAllowanceMin;

        [Space]
        [Title("Maximum value")]
        [SerializeField] private bool _useMaxValue;
        [ShowIf("@this._useMaxValue == true")]
        [SerializeField] private int _maxValue;
        [ShowIf("@this._useMaxValue == true")]
        [Tooltip("Allow operations that would increase value above max value by clamping to max value")]
        [SerializeField] private bool _allowOverflowOperationsAndClampMax = true;
        [ShowIf("@this._useMaxValue == true")]
        [Tooltip("The maximum amount an Add operation can go above the specified maximum value. Set to < 0 to allow unlimited overflow")]
        [SerializeField] private int _overflowAllowanceMax;


        // STATE
        private int _currentValue;
        private bool _isInitialised = false;

        private event Action<int> OnValueChanged;
        private event Action<object> OnValueChangedObj;

        public string Key { get { return _valueKey; } }
        /// <summary>
        /// Returns the maximum value set for this shared value, or null if no maximum is set
        /// </summary>
        public int? MaxValue { get { if (_useMaxValue) { return _maxValue; } else { return null; } } }
        /// <summary>
        /// Returns the minimum value set for this shared value, or null if no minimum is set
        /// </summary>
        public int? MinValue { get { if (_useMinValue) { return _minValue; } else { return null; } } }

        #region Lifecycle

        private void Awake()
        {
            _currentValue = _initialValue;
        }

        private void Start()
        {
            if (_initialiseOnStart)
            {
                _currentValue = _initialValue;
                BroadcastValueChanged();
                _isInitialised = true;
            }
        }

        #endregion

        #region Initialisation

        public void Initialise(SharedIntValueConfig config)
        {
            if (!_isInitialised)
            {
                _initialValue = config.InitialValue;
                _useMinValue = config.UseMinValue;
                _useMaxValue = config.UseMaxValue;
                _allowOverflowOperationsAndClampMin = config.AllowOverflowOperationsAndClampMin;
                _allowOverflowOperationsAndClampMax = config.AllowOverflowOperationsAndClampMax;
                _overflowAllowanceMax = config.OverflowAllowanceMax;
                _overflowAllowanceMin = config.OverflowAllowanceMin;
                _maxValue = config.MaxValue;
                _minValue = config.MinValue;

                _currentValue = _initialValue;
                BroadcastValueChanged();

                _isInitialised = true;
            }
        }

        #endregion

        #region Public methods

        public bool SetValue(int newValue)
        {
            if (_useMaxValue && newValue > _maxValue)
            {
                return false;
            }
            
            if(_useMinValue && newValue < _minValue)
            {
                return false;
            }

            _currentValue = newValue;

            BroadcastValueChanged();

            return true;
        }

        public bool AddValue(int valueToAdd)
        {
            if (!CanAdd(valueToAdd))
            {
                return false;
            }

            if (_useMaxValue && _allowOverflowOperationsAndClampMax && (_currentValue + valueToAdd > _maxValue))
            {
                _currentValue = _maxValue;
            }
            else
            {
                _currentValue += valueToAdd;
            }

            BroadcastValueChanged();

            return true;
        }

        public bool SubtractValue(int valueToSubtract)
        {
            if (!CanSubtract(valueToSubtract))
            {
                return false;
            }

            if (_useMinValue && (_currentValue - valueToSubtract < _minValue))
            {
                _currentValue = _minValue;
            }
            else
            {
                _currentValue -= valueToSubtract;
            }

            BroadcastValueChanged();

            return true;
        }

        public bool CanAdd(int valueToAdd)
        {
            if (valueToAdd < 0 || (_useMaxValue && _currentValue >= _maxValue))
            {
                return false;
            }

            if (_useMaxValue && ((!_allowOverflowOperationsAndClampMax && (_currentValue + valueToAdd > _maxValue)) || (_allowOverflowOperationsAndClampMax && _overflowAllowanceMax > 0 && (_currentValue + valueToAdd > _maxValue + _overflowAllowanceMax))))
            {
                return false;
            }

            return true;
        }

        public bool CanSubtract(int valueToSubtract)
        {
            if (valueToSubtract < 0 || (_useMinValue && _currentValue <= _minValue))
            {
                return false;
            }

            if (_useMinValue && ((!_allowOverflowOperationsAndClampMin && (_currentValue - valueToSubtract < _minValue)) || (_allowOverflowOperationsAndClampMin && _overflowAllowanceMin > 0 && (_currentValue - valueToSubtract < _minValue - _overflowAllowanceMin))))
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Restarting

        public void OnRestart()
        {
            _currentValue = _initialValue;
            BroadcastValueChanged();
        }

        #endregion

        #region Subscriptions

        private void BroadcastValueChanged()
        {
            OnValueChanged?.Invoke(_currentValue);
            OnValueChangedObj?.Invoke(_currentValue);
        }

        // For SharedIntValue subscribers

        public int SubscribeToValueChanged(Action<int> callback)
        {
            OnValueChanged += callback;
            return _currentValue;
        }

        public void UnsubscribeFromValueChanged(Action<int> callback)
        {
            OnValueChanged -= callback;
        }

        // For IValueProvider subscribers
        public void SubscribeToValue(string valueKey, Action<object> callback)
        {
            if (valueKey == _valueKey)
            {
                OnValueChangedObj += callback;
            }
        }

        public void UnsubscribeFromValue(string valueKey, Action<object> callback)
        {
            if (valueKey == _valueKey)
            {
                OnValueChangedObj -= callback;
            }
        }

        #endregion

    }

    public struct SharedIntValueConfig
    {
        public int InitialValue;
        public bool UseMinValue;
        public int MinValue;
        public bool AllowOverflowOperationsAndClampMin;
        public int OverflowAllowanceMin;

        public bool UseMaxValue;
        public int MaxValue;
        public bool AllowOverflowOperationsAndClampMax;
        public int OverflowAllowanceMax;
    }
}