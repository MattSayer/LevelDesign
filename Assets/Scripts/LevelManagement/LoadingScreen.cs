using AmalgamGames.Config;
using AmalgamGames.Core;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.LevelManagement
{
    public class LoadingScreen : MonoBehaviour, IValueProvider
    {
        [Title("Loading tips")]
        [SerializeField] private StringDatabase _loadingTips;
        [SerializeField] private float _tipDisplayTime = 5f;
        
        // State
        private float _currentProgress = 0;

        // Events
        private event Action<object> OnProgressChanged;
        private event Action<object> OnLoadingTipChanged;

        // Coroutines
        private Coroutine _cycleTipRoutine = null;


        #region Lifecycle
        
        private void Start()
        {
            DisplayRandomLoadingTip();
        }
        
        #endregion

        #region Loading tips
        
        private void DisplayRandomLoadingTip()
        {
            string randomTip = _loadingTips.GetRandomString();
            OnLoadingTipChanged?.Invoke(randomTip);
            _cycleTipRoutine = StartCoroutine(changeLoadingTip());
        }
        
        #endregion

        #region Coroutines
        
        private IEnumerator changeLoadingTip()
        {
            yield return new WaitForSeconds(_tipDisplayTime);
            DisplayRandomLoadingTip();
        }
        
        #endregion

        #region Value provider

        public void SubscribeToValue(string valueName, Action<object> callback)
        {
            switch(valueName)
            {
                case Globals.LOADING_PROGRESS_KEY:
                    OnProgressChanged += callback;
                    break;
                case Globals.LOADING_TIP_KEY:
                    OnLoadingTipChanged += callback;
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
                case Globals.LOADING_TIP_KEY:
                    OnLoadingTipChanged -= callback;
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