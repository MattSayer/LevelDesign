using System.Collections;
using AmalgamGames.Core;
using AmalgamGames.Editor;
using AmalgamGames.Input;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Menus
{
    public class ModelRotator : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField] private float _autoRotateSpeed = 45;
        [Space]
        [Title("Subscriptions")]
        [RequireInterface(typeof(IValueProvider))]
        [SerializeField] private UnityEngine.Object valueProvider;
        [SerializeField] private string _valueKey;
        
        private IValueProvider _valueProvider => valueProvider as IValueProvider;
        
        // State
        private bool _isSubscribedToValue = false;
        private Transform _activeModel;
        
        
        // Coroutines
        private Coroutine _autoRotateRoutine = null;
        
        #region Lifecycle
        
        private void OnEnable()
        {
            SubscribeToValue();
            if(_autoRotateRoutine == null && _activeModel != null)
            {
                _autoRotateRoutine = StartCoroutine(autoRotate());
            }
        }
        
        private void OnDisable()
        {
            UnsubscribeFromValue();
            if(_autoRotateRoutine != null)
            {
                StopCoroutine(_autoRotateRoutine);
                _autoRotateRoutine = null;
            }
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromValue();
            if(_autoRotateRoutine != null)
            {
                StopCoroutine(_autoRotateRoutine);
                _autoRotateRoutine = null;
            }
        }
        
        #endregion
        
        #region Model
        
        private void OnModelChanged(object rawValue)
        {
            if(rawValue.GetType() == typeof(Transform))
            {
                _activeModel = (Transform)rawValue;
                
                if(_autoRotateRoutine == null)
                {
                    _autoRotateRoutine = StartCoroutine(autoRotate());
                }
            }
        }
        
        #endregion
        
        #region Coroutines
        
        private IEnumerator autoRotate()
        {
            // Start autorotation on Y axis
            while(true)
            {
                _activeModel.Rotate(Vector3.up, Time.deltaTime * _autoRotateSpeed);
                yield return null;
            }
        }
        
        #endregion
        
        #region Subscriptions
        
        private void SubscribeToValue()
        {
            if(!_isSubscribedToValue)
            {
                _valueProvider.SubscribeToValue(_valueKey, OnModelChanged);
                _isSubscribedToValue = true;
            }
        }


        private void UnsubscribeFromValue()
        {
            if (_isSubscribedToValue)
            {
                _valueProvider.UnsubscribeFromValue(_valueKey, OnModelChanged);
                _isSubscribedToValue = false;
            }
        }
        
        #endregion
    }
}