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

namespace AmalgamGames.Core
{
    public class InputProcessor : MonoBehaviour, IInputProcessor
    {
        [Title("Dependency provider")]
        [SerializeField] private DependencyRequest _getInputProcessor;

        // State
        private bool _isSubscribedToDependencyRequests = false;
        private bool _isSubscribedToAnyButtonPress = false;

        #region Public interface

        public event Action<Vector2> OnCameraInputChange;
        public event Action<Vector2> OnNudgeInputChange;
        public event Action<float> OnChargeInputChange;
        public event Action<float> OnSlowmoInputChange;
        public event Action OnConfirm;
        public event Action OnAny;
        public event Action OnRespawn;
        public event Action OnRestart;
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

        public void OnCameraInput(InputAction.CallbackContext context)
        {
            Vector2 rawInput = context.ReadValue<Vector2>();
            OnCameraInputChange?.Invoke(rawInput);
        }

        public void OnSlowmoInput(InputAction.CallbackContext context)
        {
            float rawInput = context.ReadValue<float>();
            OnSlowmoInputChange?.Invoke(rawInput);
        }

        public void OnChargeInput(InputAction.CallbackContext context)
        {
            float rawInput = context.ReadValue<float>();
            OnChargeInputChange?.Invoke(rawInput);
        }

        public void OnRespawnInput(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
            {
                OnRespawn?.Invoke();
            }
        }

        public void OnNudgeInput(InputAction.CallbackContext context)
        {
            Vector2 rawInput = context.ReadValue<Vector2>();
            OnNudgeInputChange?.Invoke(rawInput);
        }

        public void OnRestartInput(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
            {
                OnRestart?.Invoke();
            }
        }

        public void OnRefillJuiceInput(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
            {
                OnRefillJuice?.Invoke();
            }
        }

        public void OnConfirmInput(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
            {
                OnConfirm?.Invoke();
            }
        }
        
        public void OnAnyInput(InputControl control)
        {
            OnAny?.Invoke();
            _isSubscribedToAnyButtonPress = false;
        }

        #endregion

        #region Subscriptions
        
        public void SubscribeToAnyButtonPress()
        {
            if(!_isSubscribedToAnyButtonPress)
            {
                InputSystem.onAnyButtonPress.CallOnce(ctrl => OnAnyInput(ctrl));
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
        public event Action<Vector2> OnCameraInputChange;
        public event Action<Vector2> OnNudgeInputChange;
        public event Action<float> OnSlowmoInputChange;
        public event Action<float> OnChargeInputChange;
        public event Action OnRespawn;
        public event Action OnRestart;
        public event Action OnRefillJuice;
    }
}