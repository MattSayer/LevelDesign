using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Core;
using AmalgamGames.Saving;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Menus
{
    public class SlotMenu : MonoBehaviour, IInitialisable
    {
        
        [Title("Save slots")]
        [SerializeField] private DataProvider[] _saveSlots;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getSaveDataManager;
        
        // Components
        private ISaveDataManager _saveDataManager;
        
        #region Lifecycle
        
        public void OnInitialisation(InitialisationPhase phase)
        {
            switch(phase)
            {
                case InitialisationPhase.Start:
                    _getSaveDataManager.RequestDependency(ReceiveSaveDataManager);
                    break;
            }
        }
        
        private void OnEnable()
        {
            if(_saveDataManager != null)
            {
                ProvideSaveData();
            }
        }
        
        #endregion
        
        #region Data providing
        
        private void ProvideSaveData()
        {
            SaveSlotSummary[] summaryData = _saveDataManager.GetSaveSlotSummary();
            for(int i = 0; i < _saveSlots.Length; i++)
            {
                DataProvider provider = _saveSlots[i];
                provider.ProvideData(new object[] { summaryData[i] });
            }
        }
        
        #endregion
        
        #region Dependencies
        
        private void ReceiveSaveDataManager(object rawObj)
        {
            _saveDataManager = rawObj as ISaveDataManager;
            // If received while gameobject is active, we need to provide data to saveslots
            if(gameObject.activeSelf)
            {
                ProvideSaveData();
            }
        }
        
        #endregion
    }
}