using AmalgamGames.Editor;
using AmalgamGames.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using AmalgamGames.Transformation;

namespace AmalgamGames.UI
{
    public class DynamicText : MonoBehaviour
    {
        [Title("Target")]
        [RequireInterface(typeof(IValueProvider))]
        [SerializeField] private UnityEngine.Object valueProvider;
        [SerializeField] private string _valueKey;
        [Space]
        [Title("Transformation")]
        [SerializeField] private ConditionalTransformation[] _transformations;
        [Title("UI")]
        [SerializeField] private TMPro.TextMeshProUGUI _text;

        // STATE
        private bool _isSubscribed = false;

        private IValueProvider _valueProvider => valueProvider as IValueProvider;

        #region Lifecycle

        private void OnEnable()
        {
            _valueProvider.SubscribeToValue(_valueKey, OnValueChanged);
            _isSubscribed = true;
        }

        private void OnDisable()
        {
            UnsubscribeFromValue();
        }

        private void OnDestroy()
        {
            UnsubscribeFromValue();
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

        private void OnValueChanged(object value)
        {
            object finalValue = value;

            foreach(ConditionalTransformation transformation in _transformations)
            {
                finalValue = transformation.TransformObject(finalValue);
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
