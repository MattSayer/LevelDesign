using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmalgamGames.Config;
using AmalgamGames.Control;
using AmalgamGames.Helpers.Classes;
using AmalgamGames.Input;
using AmalgamGames.LevelManagement;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Menus
{
    public class CharacterSelector : MonoBehaviour
    {
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getUIInputProcessor;
        [SerializeField] private DependencyRequest _getObjectSwitcher;
        [SerializeField] private DependencyRequest _getLevelManager;
        
        // State
        private bool _isSubscribedToInput = false;
        private CharacterStats[] _allCharacters;
        private int _currentCharacterIndex = 0;
        
        // Components
        private IUIInputProcessor _uiInputProcessor;
        private IObjectSwitcher _objectSwitcher;
        private ILevelManager _levelManager;
        
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
                    break;
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