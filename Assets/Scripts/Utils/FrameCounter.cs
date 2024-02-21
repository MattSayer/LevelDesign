using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Utils
{
    public class FrameCounter : ManagedBehaviour, IValueProvider
    {
        private event Action<object> OnFPSChanged;

        public override void ManagedUpdate(float deltaTime)
        {
            //Debug.Log($"Frame rate: {(1/deltaTime)}");
            float fps = 1.0f / deltaTime;
            OnFPSChanged?.Invoke(fps);
        }

        public void SubscribeToValue(string valueName, Action<object> callback)
        {
            switch(valueName)
            {
                case Globals.FPS_KEY:
                    OnFPSChanged += callback;
                    break;
            }
        }

        public void UnsubscribeFromValue(string valueName, Action<object> callback)
        {
            switch (valueName)
            {
                case Globals.FPS_KEY:
                    OnFPSChanged -= callback;
                    break;
            }
        }
    }
}