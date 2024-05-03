using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Control;
using AmalgamGames.LevelManagement;
using AmalgamGames.Saving;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace AmalgamGames.Menus
{
    public class MainMenu : MonoBehaviour
    {
        
        [Title("Components")]
        [SerializeField] private Button _continueButton;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getObjectSwitcher;
        [SerializeField] private DependencyRequest _getSaveDataManager;
        [SerializeField] private DependencyRequest _getLevelManager;
        
        // Components
        private IObjectSwitcher _objectSwitcher;
        private ISaveDataManager _saveDataManager;
        private ILevelManager _levelManager;
        
        #region Lifecycle
        
        private void Start()
        {
            _getObjectSwitcher.RequestDependency(ReceiveObjectSwitcher);
            _getSaveDataManager.RequestDependency(ReceiveSaveDataManager);
            _getLevelManager.RequestDependency(ReceiveLevelManager);
        }
        
        #endregion
        
        
        #region Button events
        
        public void OnNewGame()
        {
            _objectSwitcher.SwitchTo(Globals.NEW_GAME_PANEL);
        }
        
        public void OnContinueGame()
        {
            _levelManager.LoadScene(Globals.LEVEL_SELECT_SCENE);
        }
        
        public void OnLoadGame()
        {
            _objectSwitcher.SwitchTo(Globals.LOAD_GAME_PANEL);
        }
        
        public void OnOptions()
        {
            _objectSwitcher.SwitchTo(Globals.OPTIONS_PANEL);
        }
        
        public void OnCredits()
        {
            _objectSwitcher.SwitchTo(Globals.CREDITS_PANEL);
        }
        
        public void OnQuit()
        {
            Application.Quit();
        }
        
        #endregion
        
        #region Saving
        
        private void CheckSaveFilesExist()
        {
            bool saveFilesExist = _saveDataManager.DoSaveFilesExist();
            
            if(saveFilesExist)
            {
                // Make continue game default selected button
                _continueButton.Select();
            }
            else
            {
                // Disable continue button if no save files exist
                _continueButton.interactable = false;
            }
        }
        
        
        #endregion
        
        #region Dependencies
        
        private void ReceiveObjectSwitcher(object rawObj)
        {
            _objectSwitcher = rawObj as IObjectSwitcher;
        }
        
        private void ReceiveSaveDataManager(object rawObj)
        {
            _saveDataManager = rawObj as ISaveDataManager;
            CheckSaveFilesExist();
        }
        
        private void ReceiveLevelManager(object rawObj)
        {
            _levelManager = rawObj as ILevelManager;
        }
        
        #endregion
        
    }
}