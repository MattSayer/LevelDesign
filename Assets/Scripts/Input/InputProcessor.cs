using AmalgamGames.Core;
using AmalgamGames.Timing;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace AmalgamGames.Input
{
    public class InputProcessor : MonoBehaviour, IInputProcessor
    {
        [Title("Dependency provider")]
        [SerializeField] private DependencyRequest _getInputProcessor;

        // State
        private bool _isSubscribedToDependencyRequests = false;
        private bool _isSubscribedToAnyButtonPress = false;

        #region Public interface

        public event Action<Vector2> OnCameraInput;
        public event Action<Vector2> OnNudgeInput;
        public event Action<float> OnChargeInput;
        public event Action<float> OnSlowmoInput;
        public event Action OnConfirmInput;
        public event Action OnAnyInput;
        public event Action OnRespawnInput;
        public event Action OnRestartInput;
        public event Action OnRefillJuice;

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

        public void OnCameraInputChange(InputAction.CallbackContext context)
        {
            Vector2 rawInput = context.ReadValue<Vector2>();
            OnCameraInput?.Invoke(rawInput);
        }

        public void OnSlowmoInputChange(InputAction.CallbackContext context)
        {
            float rawInput = context.ReadValue<float>();
            OnSlowmoInput?.Invoke(rawInput);
        }

        public void OnChargeInputChange(InputAction.CallbackContext context)
        {
            float rawInput = context.ReadValue<float>();
            OnChargeInput?.Invoke(rawInput);
        }

        public void OnRespawnInputChange(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                OnRespawnInput?.Invoke();
            }
        }

        public void OnNudgeInputChange(InputAction.CallbackContext context)
        {
            Vector2 rawInput = context.ReadValue<Vector2>();
            OnNudgeInput?.Invoke(rawInput);
        }

        public void OnRestartInputChange(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
            {
                OnRestartInput?.Invoke();
            }
        }

        public void OnRefillJuiceInput(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
            {
                OnRefillJuice?.Invoke();
            }
        }

        public void OnConfirmInputChange(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
            {
                OnConfirmInput?.Invoke();
            }
        }
        
        public void OnAnyInputChange(InputControl control)
        {
            OnAnyInput?.Invoke();
            _isSubscribedToAnyButtonPress = false;
        }

        #endregion

        #region Subscriptions
        
        public void SubscribeToAnyButtonPress()
        {
            if(!_isSubscribedToAnyButtonPress)
            {
                InputSystem.onAnyButtonPress.CallOnce(ctrl => OnAnyInputChange(ctrl));
                _isSubscribedToAnyButtonPress = true;
            }
        }
        
        #endregion

        #region Dependency requests

        private void ProvideDependency(Action<object> callback)
        {
            callback?.Invoke((IInputProcessor)this);
        }

        private void SubscribeToDependencyRequests()
        {
            if (!_isSubscribedToDependencyRequests)
            {
                _getInputProcessor.OnDependencyRequest += ProvideDependency;
                _isSubscribedToDependencyRequests = true;
            }
        }

        private void UnsubscribeFromDependencyRequests()
        {
            if (_isSubscribedToDependencyRequests)
            {
                _getInputProcessor.OnDependencyRequest -= ProvideDependency;
                _isSubscribedToDependencyRequests = false;
            }
        }

        #endregion

    }

    public interface IInputProcessor
    {
        public event Action<Vector2> OnCameraInput;
        public event Action<Vector2> OnNudgeInput;
        public event Action<float> OnSlowmoInput;
        public event Action<float> OnChargeInput;
        public event Action OnRespawnInput;
        public event Action OnRestartInput;
        public event Action OnRefillJuice;
    }
}