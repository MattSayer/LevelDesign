using AmalgamGames.Editor;
using AmalgamGames.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace AmalgamGames.UI
{
    public class DynamicFilledImage : MonoBehaviour
    {
        [Title("Expected values")]
        [SerializeField] private float _inputMin = 0;
        [SerializeField] private float _inputMax = 100;
        [Space]
        [Title("Target")]
        [RequireInterface(typeof(IValueProvider))]
        [SerializeField] private UnityEngine.Object valueProvider;
        [SerializeField] private string _valueKey;
        [Space]
        [Title("UI")]
        [SerializeField] private Image _image;

        // STATE
        private bool _isSubscribed = false;
        private float _inputRange;

        private int FILL_PROP = Shader.PropertyToID("_Fill");

        private Material _imageMaterial;

        private IValueProvider _valueProvider => valueProvider as IValueProvider;


        #region Lifecycle

        private void Awake()
        {
            _inputRange = _inputMax - _inputMin;
            // Create new instance of material so property changes aren't shared
            _imageMaterial = Instantiate(_image.material);
            _image.material = _imageMaterial;
        }

        private void OnEnable()
        {
            SubscribeToValue();
        }

        private void OnDisable()
        {
            UnsubscribeFromValue();
        }

        private void OnDestroy()
        {
            UnsubscribeFromValue();
        }

        #endregion

        #region Dynamic value

        private void OnValueChanged(object rawValue)
        {
            if(rawValue.GetType() == typeof(float))
            {
                float floatVal = (float)rawValue;

                float normalizedVal = ((floatVal - _inputMin) / _inputRange);

                _imageMaterial.SetFloat(FILL_PROP, normalizedVal);
            }
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

    }
}