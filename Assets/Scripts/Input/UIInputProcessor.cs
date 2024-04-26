using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Helpers.Classes;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmalgamGames.Input
{
    public class UIInputProcessor : MonoBehaviour, IUIInputProcessor
    {
        [Title("Dependency provider")]
        [SerializeField] private DependencyRequest _getUIInputProcessor;
        
        // State
        private bool _isSubscribedToDependencyRequests = false;
        
        #region Events
        
        public event Action OnConfirmInput;
        public event Action OnBackInput;
        public event Action<FlatDirection> OnTabInput;
        public event Action<FlatDirection> OnLeftRightInput;
        public event Action<Vector2> OnRotationInput;
        
        #endregion
        
        #region Lifecycle

        private void Awake()
        {
            SubscribeToDependencyRequests();
        }

        private void OnEnable()
        {
            SubscribeToDependencyRequests();
        }

        private void OnDisable()
        {
            UnsubscribeFromDependencyRequests();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDependencyRequests();
        }

        #endregion
        
        #region Input events
        
        public void OnBackInputChange(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
            {
                OnBackInput?.Invoke();
            }
        }
        
        public void OnTabInputChange(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
            {
                float inputVal = context.ReadValue<float>();
                
                OnTabInput?.Invoke(inputVal > 0 ? FlatDirection.Right : FlatDirection.Left);
            }
        }
        
        public void OnLeftRightInputChange(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
            {
                float inputVal = context.ReadValue<float>();
                
                OnTabInput?.Invoke(inputVal > 0 ? FlatDirection.Right : FlatDirection.Left);
            }
        }
        
        public void OnConfirmInputChange(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
            {
                OnConfirmInput?.Invoke();
            }
        }
        
        public void OnRotationInputChange(InputAction.CallbackContext context)
        {
            Vector2 rawInput = context.ReadValue<Vector2>();
            OnRotationInput?.Invoke(rawInput);
        }
        
        #endregion
        
        
        #region Dependency requests

        private void ProvideDependency(Action<object> callback)
        {
            callback?.Invoke((IUIInputProcessor)this);
        }

        private void SubscribeToDependencyRequests()
        {
            if (!_isSubscribedToDependencyRequests)
            {
                _getUIInputProcessor.OnDependencyRequest += ProvideDependency;
                _isSubscribedToDependencyRequests = true;
            }
        }

        private void UnsubscribeFromDependencyRequests()
        {
            if (_isSubscribedToDependencyRequests)
            {
                _getUIInputProcessor.OnDependencyRequest -= ProvideDependency;
                _isSubscribedToDependencyRequests = false;
            }
        }

        #endregion
    }
    
    public interface IUIInputProcessor
    {
        public event Action OnConfirmInput;
        public event Action OnBackInput;
        public event Action<FlatDirection> OnTabInput;
        public event Action<FlatDirection> OnLeftRightInput;
        public event Action<Vector2> OnRotationInput;
        
    }
}