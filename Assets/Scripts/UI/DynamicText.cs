using AmalgamGames.Editor;
using AmalgamGames.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using AmalgamGames.Transformation;
using AmalgamGames.Utils;
using AmalgamGames.UpdateLoop;

namespace AmalgamGames.UI
{
    public class DynamicText : MonoBehaviour, IInitialisable
    {
        [Title("Target")]
        [RequireInterface(typeof(IValueProvider))]
        [SerializeField] private UnityEngine.Object valueProvider;
        [SerializeField] private string _valueKey;
        [Space]
        [Title("Transformation")]
        [SerializeField] private ConditionalTransformationGroup[] _transformations;
        [Title("UI")]
        [SerializeField] private TMPro.TextMeshProUGUI _text;
        [SerializeField] private string _defaultValue = "";
        [Space]
        [Title("Settings")]
        [SerializeField] private bool _subscribeOnStart = true;
        [SerializeField] private bool _resetToDefaultValueOnEnable = true;
        [SerializeField] private bool _staySubscribedWhileDisabled = true;

        // STATE
        private bool _isSubscribed = false;

        private IValueProvider _valueProvider => valueProvider as IValueProvider;

        #region Lifecycle

        public void OnInitialisation(InitialisationPhase phase)
        {
            switch(phase)
            {
                case InitialisationPhase.Start:
                    if(_subscribeOnStart)
                    {
                        SubscribeToValue();
                    }
                    break;
            }
        }

        private void Start()
        {
            if(_subscribeOnStart)
            {
                SubscribeToValue();
            }
        }

        private void OnEnable()
        {
            if(_resetToDefaultValueOnEnable)
            {
                ResetToDefaultValue();
            }
            SubscribeToValue();
        }

        private void OnDisable()
        {
            if(!_staySubscribedWhileDisabled)
            {
                UnsubscribeFromValue();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromValue();
        }
        
        #endregion
        
        #region Subscriptions
        
        private void SubscribeToValue()
        {
            if(!_isSubscribed)
            {
                _valueProvider.SubscribeToValue(_valueKey, OnValueChanged);
                _isSubscribed = true;
            }
        }


        private void UnsubscribeFromValue()
        {
            if (_isSubscribed)
            {
                _valueProvider.UnsubscribeFromValue(_valueKey, OnValueChanged);
                _isSubscribed = false;
            }
        }

        #endregion

        #region UI

        private void ResetToDefaultValue()
        {
            _text.text = _defaultValue;
        }

        private void OnValueChanged(object value)
        {
            object finalValue = value;

            for(int i = 0; i < _transformations.Length; i++)
            {
                ConditionalTransformationGroup t = _transformations[i];
                finalValue = t.TransformObject(finalValue);
            }

            _text.text = Convert.ToString(finalValue);
        }

        #endregion

    }

    public enum ConversionType
    {
        String,
        Integer,
        Float,
        Date
    }
}
