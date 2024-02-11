using AmalgamGames.Editor;
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
        [Space]
        [Title("Settings")]
        [SerializeField] private ConversionType _convertTo;
        [SerializeField] private string _formatString;
        [Space]
        [Title("UI")]
        [SerializeField] private TMPro.TextMeshProUGUI _text;
        

        private IValueProvider _valueProvider => valueProvider as IValueProvider;

        #region Lifecycle

        private void OnEnable()
        {
            _valueProvider.OnValueChanged += OnValueChanged;
        }

        private void OnDisable()
        {
            _valueProvider.OnValueChanged -= OnValueChanged;
        }

        private void OnDestroy()
        {
            _valueProvider.OnValueChanged -= OnValueChanged;
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

    public interface IValueProvider
    {
        public event System.Action<object> OnValueChanged;
    }

    public enum ConversionType
    {
        String,
        Integer,
        Float,
        Date
    }
}