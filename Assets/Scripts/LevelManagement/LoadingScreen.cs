using AmalgamGames.Core;
using AmalgamGames.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace AmalgamGames.LevelManagement
{
    public class LoadingScreen : MonoBehaviour, IValueProvider
    {
        // State
        private float _currentProgress = 0;

        private event Action<object> OnProgressChanged;

        #region Value provider

        public void SubscribeToValue(string valueName, Action<object> callback)
        {
            switch(valueName)
            {
                case Globals.LOADING_PROGRESS_KEY:
                    OnProgressChanged += callback;
                    break;
            }
        }

        public void UnsubscribeFromValue(string valueName, Action<object> callback)
        {
            switch (valueName)
            {
                case Globals.LOADING_PROGRESS_KEY:
                    OnProgressChanged -= callback;
                    break;
            }
        }

        #endregion
        
        #region Progress management

        public void UpdateProgress(float progress)
        {
            _currentProgress = progress;
        }

        #endregion

    }
}