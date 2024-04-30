using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using AmalgamGames.Config;
using AmalgamGames.Control;
using AmalgamGames.Effects;
using AmalgamGames.Helpers.Classes;
using AmalgamGames.Input;
using AmalgamGames.LevelManagement;
using AmalgamGames.Saving;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Menus
{
    public class LevelSelect : MonoBehaviour
    {
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getLevelManager;
        [SerializeField] private DependencyRequest _getSaveDataManager;
        [SerializeField] private DependencyRequest _getObjectSwitcher;
        [SerializeField] private DependencyRequest _getUIInputProcessor;
        
        // Events
        public event Action<bool> OnThemeChanged;
        public event Action<object> OnLevelSelected;
        
        // Components
        private ILevelManager _levelManager;
        private ISaveDataManager _saveDataManager;
        private IObjectSwitcher _objectSwitcher;
        private IUIInputProcessor _uiInputProcessor;
        
        // State
        private bool _isSubscribedToInput = false;
        private int _currentThemeIndex = 0;
        private ThemeConfig _currentTheme;
        private SaveData _currentSaveData;
        
        // Public accessors
        public ThemeConfig CurrentTheme { get { return _currentTheme; } }
        public SaveData CurrentSaveData { get { return _currentSaveData; } }
        
        #region Lifecycle

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
        
        private void Start()
        {
            StartDependencyRequestChain();
            
            // ObjectSwitcher and UIInputProcessor are not required for initialisation, so we can request
            // the dependencies separately
            _getObjectSwitcher.RequestDependency(ReceiveObjectSwitcher);
            _getUIInputProcessor.RequestDependency(ReceiveUIInputProcessor);
        }
        
        #endregion
        
        #region Initialisation
        
        private void InitialiseMenu()
        {
            // Default current theme to the last theme played
            _currentSaveData = _saveDataManager.CurrentSaveData;
            _currentThemeIndex = _currentSaveData.LastThemePlayed;
            _currentTheme = _levelManager.LevelHierarchyConfig.Themes[_currentThemeIndex];
            _levelManager.ChangeCurrentTheme(_currentThemeIndex);
            
            OnThemeChanged?.Invoke(false);
            
            // Check whether player has unlocked a new theme since LevelSelect was last loaded
            if(_currentSaveData.HasNewTheme)
            {
                // Play unlock animation for new theme
                // New theme is last in array with unlocked star requirement
                
                int totalStarsUnlocked = _currentSaveData.TotalStarsUnlocked;
                
                ThemeConfig latestConfig = _currentTheme;
                int latestThemeIndex = _currentThemeIndex;
                for(int i = _currentThemeIndex + 1; i < _levelManager.LevelHierarchyConfig.Themes.Length; i++)
                {
                    ThemeConfig thisConfig = _levelManager.LevelHierarchyConfig.Themes[i];
                    if(totalStarsUnlocked >= thisConfig.StarRequirement)
                    {
                        latestConfig = thisConfig;
                        latestThemeIndex = i;
                    }
                }
                
                _currentThemeIndex = latestThemeIndex;
                _currentTheme = latestConfig;
                _levelManager.ChangeCurrentTheme(_currentThemeIndex);
                
                OnThemeChanged?.Invoke(true);
                
                // reset to false
                _currentSaveData.HasNewTheme = false;
            }
            
        }
        
        #endregion
        
        #region Theme
        
        public void ChangeTheme(TallyOperation tallyOperation)
        {
            switch(tallyOperation)
            {
                case TallyOperation.Increment:
                    if(_currentThemeIndex >= _levelManager.LevelHierarchyConfig.Themes.Length - 1)
                    {
                        _currentThemeIndex = 0;
                    }
                    else
                    {
                        _currentThemeIndex++;
                    }
                    break;
                case TallyOperation.Decrement:
                    if(_currentThemeIndex == 0)
                    {
                        _currentThemeIndex = _levelManager.LevelHierarchyConfig.Themes.Length - 1;
                    }
                    else
                    {
                        _currentThemeIndex--;
                    }
                    break;
            }
            
            _currentTheme = _levelManager.LevelHierarchyConfig.Themes[_currentThemeIndex];
            
            OnThemeChanged?.Invoke(false);
        }
        
        #endregion
        
        #region Input
        
        public void BackToMainMenu()
        {
            _levelManager.LoadScene(Globals.MAIN_MENU_SCENE);
        }
        
        private void ChangeTheme(FlatDirection direction)
        {
            switch(direction)
            {
                case FlatDirection.Left:
                    ChangeTheme(TallyOperation.Decrement);
                    break;
                case FlatDirection.Right:
                    ChangeTheme(TallyOperation.Increment);
                    break;
            }
        }
        
        #endregion
        
        #region Level selection
        
        public void SelectLevel(int levelIndex, LevelConfig levelConfig, LevelSaveData levelSaveData)
        {
            // Provide selected level data for data providers
            // Raise two separate events, since DynamicEvents are set up for non-array object parameters
            OnLevelSelected?.Invoke(levelConfig);
            OnLevelSelected?.Invoke(levelSaveData);
            
            // Set selected level and level index on LevelManager, for use if 
            // player clicks the Play button
            _levelManager.SetSelectedLevel(levelConfig);
            _levelManager.UpdateCurrentLevelIndex(levelIndex);
            
            // Switch to Level Details panel
            _objectSwitcher.SwitchTo(Globals.LEVEL_DETAILS_PANEL);
        }
        
        #endregion
        
        #region Subscriptions
        
        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _uiInputProcessor != null)
            {
                _uiInputProcessor.OnBackInput -= BackToMainMenu;
                _uiInputProcessor.OnTabInput -= ChangeTheme;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _uiInputProcessor != null)
            {
                _uiInputProcessor.OnBackInput += BackToMainMenu;
                _uiInputProcessor.OnTabInput += ChangeTheme;
                _isSubscribedToInput = true;
            }
        }
        
        #endregion
        
        #region Dependencies
        
        private void ReceiveObjectSwitcher(object rawObj)
        {
            _objectSwitcher = rawObj as IObjectSwitcher;
        }
        
        private void ReceiveUIInputProcessor(object rawObj)
        {
            _uiInputProcessor = rawObj as IUIInputProcessor;
            SubscribeToInput();
        }
        
        private void StartDependencyRequestChain()
        {
            _getLevelManager.RequestDependency(ReceiveLevelManager);
        }
        
        private void ReceiveLevelManager(object rawObj)
        {
            _levelManager = rawObj as ILevelManager;
            _getSaveDataManager.RequestDependency(ReceiveSaveDataManager);
        }
        
        private void ReceiveSaveDataManager(object rawObj)
        {
            _saveDataManager = rawObj as ISaveDataManager;
            
            // Now that both level manager and save data manager references are initialised
            // we can proceed with initialising the level select menu
            InitialiseMenu();
        }
        
        #endregion
    }
}