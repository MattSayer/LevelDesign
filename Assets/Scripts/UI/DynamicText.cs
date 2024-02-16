using AmalgamGames.Editor;
using AmalgamGames.Core;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace AmalgamGames.UI
{
    public class DynamicText : MonoBehaviour
    {
        [Title("Target")]
        [RequireInterface(typeof(IValueProvider))]
        [SerializeField] private UnityEngine.Object valueProvider;
        [SerializeField] private string _valueKey;
        [Space]
        [Title("Settings")]
        [SerializeField] private ConversionType _convertTo;
        [SerializeField] private string _formatString;
        [Space]
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
            string finalString = "";

            switch(_convertTo)
            {
                case ConversionType.Date:
                    finalString = _formatString.Length > 0 ? string.Format(_formatString, (DateTime)value) : ((DateTime)value).ToString();
                    break;
                case ConversionType.String:
                    finalString = _formatString.Length > 0 ? string.Format(_formatString, value.ToString()) : value.ToString();
                    break;
                case ConversionType.Integer:
                    finalString = _formatString.Length > 0 ? string.Format(_formatString, (int)value) : ((int)value).ToString();
                    break;
                case ConversionType.Float:
                    finalString = _formatString.Length > 0 ? string.Format(_formatString, (float)value) : ((float)value).ToString();
                    break;
            }

            _text.text = finalString;
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
