using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AmalgamGames.Config;
using AmalgamGames.Helpers.Classes;
using AmalgamGames.Saving;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.LevelManagement
{
    public class LevelSelect : MonoBehaviour
    {
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getLevelManager;
        [SerializeField] private DependencyRequest _getSaveDataManager;
        [SerializeField] private DependencyRequest _getUISwitcher;
        
        // Events
        public event Action OnThemeChanged;
        public event Action<object[]> OnLevelSelected;
        
        // Components
        private ILevelManager _levelManager;
        private ISaveDataManager _saveDataManager;
        
        // State
        private int _currentThemeIndex = 0;
        private ThemeConfig _currentTheme;
        private SaveData _currentSaveData;
        
        // Public accessors
        public ThemeConfig CurrentTheme { get { return _currentTheme; } }
        public SaveData CurrentSaveData { get { return _currentSaveData; } }
        
        #region Lifecycle
        
        private void Start()
        {
            StartDependencyRequestChain();
        }
        
        #endregion
        
        #region Initialisation
        
        private void InitialiseMenu()
        {
            // Default current theme to the last theme played
            _currentThemeIndex = _saveDataManager.CurrentSaveData.LastThemePlayed;
            _currentTheme = _levelManager.LevelHierarchyConfig.Themes[_currentThemeIndex];
            _levelManager.ChangeCurrentTheme(_currentThemeIndex);
            
            OnThemeChanged?.Invoke();
            
            
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
            
            OnThemeChanged?.Invoke();
        }
        
        #endregion
        
        #region Levels
        
        public void SelectLevel(LevelConfig levelConfig, LevelSaveData levelSaveData)
        {
            // Provide selected level data for data providers
            OnLevelSelected?.Invoke(new object[] {levelConfig, levelSaveData});
            
            // Switch to Level Details panel
            
        }
        
        #endregion
        
        #region Dependencies
        
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