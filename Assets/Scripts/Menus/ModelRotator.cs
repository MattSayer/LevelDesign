using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Core;
using AmalgamGames.Editor;
using AmalgamGames.Input;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace AmalgamGames.Menus
{
    public class ModelRotator : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField] private float _rotateSpeed = 90;
        [SerializeField] private float _autoRotateDelay = 1f;
        [SerializeField] private float _autoRotateSpeed = 45;
        [SerializeField] private float _autoRotateRevertTime = 1f;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getUIInputProcessor;
        [Space]
        [Title("Subscriptions")]
        [RequireInterface(typeof(IValueProvider))]
        [SerializeField] private UnityEngine.Object valueProvider;
        [SerializeField] private string _valueKey;
        
        private IValueProvider _valueProvider => valueProvider as IValueProvider;
        
        // Components
        private IUIInputProcessor _uiInputProcessor;
        
        // State
        private bool _isSubscribedToInput = false;
        private bool _isSubscribedToValue = false;
        private Transform _activeModel;
        private Vector3 _cameraRight;
        
        // Coroutines
        private Coroutine _autoRotateRoutine = null;
        
        #region Lifecycle
        
        private void Start()
        {
            _getUIInputProcessor.RequestDependency(ReceiveUIInputProcessor);
            _cameraRight = Camera.main.transform.right;
        }
        
         private void OnEnable()
        {
            SubscribeToInput();
            SubscribeToValue();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromInput();
            UnsubscribeFromValue();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromInput();
            UnsubscribeFromValue();
        }
        
        #endregion
        
        #region Rotation
        
        private void OnRotationInput(Vector2 input)
        {
            if(_autoRotateRoutine != null)
            {
                StopCoroutine(_autoRotateRoutine);
            }
            
            if(input == Vector2.zero)
            {
                _autoRotateRoutine = StartCoroutine(autoRotate());
            }
            else
            {
                float deltaTime = Time.deltaTime;
                _activeModel.Rotate(_cameraRight, input.y * deltaTime * _rotateSpeed);
                _activeModel.Rotate(Vector3.up, input.x * deltaTime * _rotateSpeed);
            }
        }
        
        #endregion
        
        #region Model
        
        private void OnModelChanged(object rawValue)
        {
            if(rawValue.GetType() == typeof(Transform))
            {
                _activeModel = (Transform)rawValue;
            }
        }
        
        #endregion
        
        #region Coroutines
        
        private IEnumerator autoRotate()
        {
            yield return new WaitForSeconds(_autoRotateDelay);
            
            // Rotate to default rotation
            
            float rotateLerp = 0;
            Quaternion startRotation = _activeModel.rotation;
            Quaternion targetRotation = Quaternion.identity;
            while(rotateLerp < _autoRotateRevertTime)
            {
                _activeModel.rotation = Quaternion.Lerp(startRotation, targetRotation, rotateLerp / _autoRotateRevertTime);
                rotateLerp += Time.deltaTime;
                yield return null;
            }
            
            _activeModel.rotation = targetRotation;
            
            // Start autorotation on Y axis
            
            while(true)
            {
                _activeModel.Rotate(Vector3.up, Time.deltaTime * _autoRotateSpeed);
                yield return null;
            }
        }
        
        #endregion
        
        #region Subscriptions
        
        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _uiInputProcessor != null)
            {
                _uiInputProcessor.OnRotationInput -= OnRotationInput;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _uiInputProcessor != null)
            {
                _uiInputProcessor.OnRotationInput += OnRotationInput;
                _isSubscribedToInput = true;
            }
        }
        
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
        
        #region Dependencies
        
        private void ReceiveUIInputProcessor(object rawObj)
        {
            _uiInputProcessor = rawObj as IUIInputProcessor;
            SubscribeToInput();
        }
        
        #endregion
        
    }
}