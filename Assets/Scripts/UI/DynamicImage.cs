using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using AmalgamGames.Core;
using AmalgamGames.Editor;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace AmalgamGames
{
    public class DynamicImage : MonoBehaviour
    {
        
        [Title("Target")]
        [RequireInterface(typeof(IValueProvider))]
        [SerializeField] private UnityEngine.Object valueProvider;
        [SerializeField] private string _valueKey;
        [Space]
        [Title("UI")]
        [SerializeField] private Image _image;

        // STATE
        private bool _isSubscribed = false;
        private IValueProvider _valueProvider => valueProvider as IValueProvider;


        #region Lifecycle

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
            if(rawValue.GetType() == typeof(Sprite))
            {
                _image.sprite = (Sprite)rawValue;
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