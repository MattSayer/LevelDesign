using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmalgamGames.Config;
using AmalgamGames.Control;
using AmalgamGames.Core;
using AmalgamGames.Helpers.Classes;
using AmalgamGames.Input;
using AmalgamGames.LevelManagement;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Menus
{
    public class CharacterSelector : MonoBehaviour, IValueProvider
    {
        [Title("Settings")]
        [SerializeField] private float _modelSwitchTime;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getUIInputProcessor;
        [SerializeField] private DependencyRequest _getObjectSwitcher;
        [SerializeField] private DependencyRequest _getLevelManager;
        
        // Events
        public event Action<object> OnCharacterChanged;
        
        // State
        private bool _isSubscribedToInput = false;
        private CharacterStats[] _allCharacters;
        private int _currentCharacterIndex = 0;
        private bool _canChangeCharacter = true;
        
        // Components
        private IUIInputProcessor _uiInputProcessor;
        private IObjectSwitcher _objectSwitcher;
        private ILevelManager _levelManager;
        
        // Coroutines
        private Coroutine _changeRoutine = null;
        
        #region Lifecycle
        
        private void Start()
        {
            _getUIInputProcessor.RequestDependency(ReceiveUIInputProcessor);
            _getObjectSwitcher.RequestDependency(ReceiveObjectSwitcher);
            _getLevelManager.RequestDependency(ReceiveLevelManager);
            
            _allCharacters = CharacterStats.GetAllCharacters();
        }
        
        private void OnEnable()
        {
            SubscribeToInput();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromInput();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromInput();
        }
        
        #endregion
        
        #region Input
        
        private void ChangeCharacter(FlatDirection direction)
        {
            if(!_canChangeCharacter || _changeRoutine != null)
            {
                return;
            }
            
            bool wasChanged = false;
            switch(direction)
            {
                case FlatDirection.Left:
                    if(_currentCharacterIndex <= 0)
                    {
                        _currentCharacterIndex = _allCharacters.Length - 1;
                    }
                    else
                    {
                        _currentCharacterIndex--;
                    }
                    wasChanged = true;
                    break;
                case FlatDirection.Right:
                    if(_currentCharacterIndex >= _allCharacters.Length - 1)
                    {
                        _currentCharacterIndex = 0;
                    }
                    else
                    {
                        _currentCharacterIndex++;
                    }
                    wasChanged = true;
                    break;
            }
            
            if(wasChanged)
            {
                OnCharacterChanged?.Invoke(_allCharacters[_currentCharacterIndex]);
                _canChangeCharacter = false;
                _changeRoutine = StartCoroutine(characterChangeTimeout());
            }
            
        }
        
        private void SwitchBack()
        {
            _objectSwitcher.SwitchBack();
        }
        
        private void LoadLevel()
        {
            _levelManager.SetCharacter(_allCharacters[_currentCharacterIndex].Character);
            _levelManager.LoadSelectedLevel();
        }
        
        #endregion
        
        #region Coroutines
        
        private IEnumerator characterChangeTimeout()
        {
            yield return new WaitForSeconds(_modelSwitchTime);
            _canChangeCharacter = true;
            _changeRoutine = null;
        }
        
        #endregion
        
        #region Value provider
        
        public void SubscribeToValue(string key, Action<object> callback)
        {
            switch(key)
            {
                case Globals.CHARACTER_CHANGED_KEY:
                    OnCharacterChanged += callback;
                    break;
            }
        }
        
        public void UnsubscribeFromValue(string key, Action<object> callback)
        {
            switch(key)
            {
                case Globals.CHARACTER_CHANGED_KEY:
                    OnCharacterChanged -= callback;
                    break;
            }
        }
        
        #endregion
        
        #region Subscriptions
        
        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _uiInputProcessor != null)
            {
                _uiInputProcessor.OnBackInput -= SwitchBack;
                _uiInputProcessor.OnLeftRightInput -= ChangeCharacter;
                _uiInputProcessor.OnConfirmInput -= LoadLevel;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _uiInputProcessor != null)
            {
                _uiInputProcessor.OnBackInput += SwitchBack;
                _uiInputProcessor.OnLeftRightInput += ChangeCharacter;
                _uiInputProcessor.OnConfirmInput += LoadLevel;
                _isSubscribedToInput = true;
            }
        }
        
        #endregion
        
        #region Dependencies
        
        private void ReceiveUIInputProcessor(object rawObj)
        {
            _uiInputProcessor = rawObj as IUIInputProcessor;
            SubscribeToInput();
        }
        private void ReceiveObjectSwitcher(object rawObj)
        {
            _objectSwitcher = rawObj as IObjectSwitcher;
        }
        
        private void ReceiveLevelManager(object rawObj)
        {
            _levelManager = rawObj as ILevelManager;
        }
        
        #endregion
    }
}