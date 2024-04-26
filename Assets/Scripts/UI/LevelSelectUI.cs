using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Config;
using AmalgamGames.Core;
using AmalgamGames.LevelManagement;
using AmalgamGames.Saving;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace AmalgamGames.UI
{
    public class LevelSelectUI : MonoBehaviour
    {
        [Title("Components")]
        [SerializeField] private LevelSelect _levelSelect;
        [Space]
        [Title("Prefabs")]
        [SerializeField] private GameObject _levelThumbnailPrefab;
        [SerializeField] private GameObject _lockPrefab;
        [Space]
        [Title("Containers")]
        [SerializeField] private RectTransform _themeContainer;
        
        
        #region Lifecycle
        
        private void Awake() 
        {
            _levelSelect.OnThemeChanged += OnThemeChanged;
        }
        
        #endregion
        
        #region Theme changing
        
        private void OnThemeChanged(bool wasJustUnlocked)
        {
            // Get current ThemeConfig and LevelSaveData
            ThemeConfig currentTheme = _levelSelect.CurrentTheme;
            SaveData saveData = _levelSelect.CurrentSaveData;
            
            // Clear existing content
            for(int i = 0; i < _themeContainer.childCount; i++)
            {
                Destroy(_themeContainer.GetChild(i));
            }
            
            bool isThemeUnlocked = saveData.TotalStarsUnlocked >= currentTheme.StarRequirement;
            
            if(!isThemeUnlocked)
            {
                GameObject lockObject = Instantiate(_lockPrefab);
                DataProvider lockProvider = lockObject.GetComponent<DataProvider>();
                
                lockProvider.ProvideData(new object[] { currentTheme });
                
            }
            else
            {
                // Play special animation for just unlocking a new theme
                if(wasJustUnlocked)
                {
                    // TODO
                }
                
                // Iterate over levels in theme, instantiate and initialise level thumbnail prefab
                for(int i = 0; i < currentTheme.Levels.Length; i++)
                {
                    LevelConfig level = currentTheme.Levels[i];
                    LevelSaveData levelSaveData = Tools.GetLevelSaveData(saveData, level.LevelID);
                    object[] dataToProvide = new object[] { level, levelSaveData };
                    
                    // Determine whether level is unlocked yet
                    // Theme needs to be unlocked, and either level is first in theme or previous level
                    // has been completed
                    bool isLevelUnlocked = isThemeUnlocked;
                    if(i > 0)
                    {
                        LevelSaveData prevLevelSaveData = Tools.GetLevelSaveData(saveData, currentTheme.Levels[i-1].LevelID);
                        
                        if(prevLevelSaveData == null || !prevLevelSaveData.HasBeenCompleted)
                        {
                            isLevelUnlocked = false;
                        }
                    }
                    
                    GameObject levelThumbnail = Instantiate(_levelThumbnailPrefab, _themeContainer);
                    DataProvider dataProvider = levelThumbnail.GetComponent<DataProvider>();
                    dataProvider.ProvideData(dataToProvide);
                    
                    int levelIndex = i;
                    
                    if(isLevelUnlocked)
                    {
                        ClickProxy clickProxy = levelThumbnail.GetComponent<ClickProxy>();
                        clickProxy.OnButtonClicked += () => _levelSelect.SelectLevel(levelIndex, level, levelSaveData);
                    }
                    else
                    {
                        // Disable button interaction if level isn't unlocked yet
                        Button button = levelThumbnail.GetComponent<Button>();
                        button.interactable = false;
                    }
                    
                }
            }
            
        }
        
        #endregion
        
        
    }
}