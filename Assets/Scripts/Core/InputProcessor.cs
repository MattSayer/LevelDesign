using AmalgamGames.Core;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AmalgamGames.Core
{
    public class InputProcessor : MonoBehaviour, IInputProcessor
    {
        #region Public interface

        public event Action<Vector2> OnCameraInputChange;
        public event Action<Vector2> OnNudgeInputChange;
        public event Action<float> OnSlowMoChargeInputChange;
        public event Action<float> OnRealtimeChargeInputChange;
        public event Action OnRespawn;
        

        #endregion

        #region Input events

        public void OnCameraInput(InputAction.CallbackContext context)
        {
            Vector2 rawInput = context.ReadValue<Vector2>();
            OnCameraInputChange?.Invoke(rawInput);
        }

        public void OnSlowMoChargeInput(InputAction.CallbackContext context)
        {
            float rawInput = context.ReadValue<float>();
            OnSlowMoChargeInputChange?.Invoke(rawInput);
        }

        public void OnRealtimeChargeInput(InputAction.CallbackContext context)
        {
            float rawInput = context.ReadValue<float>();
            OnRealtimeChargeInputChange?.Invoke(rawInput);
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

        #endregion
       
    }

    public interface IInputProcessor
    {
        public event Action<Vector2> OnCameraInputChange;
        public event Action<Vector2> OnNudgeInputChange;
        public event Action<float> OnSlowMoChargeInputChange;
        public event Action<float> OnRealtimeChargeInputChange;
        public event Action OnRespawn;
    }
}