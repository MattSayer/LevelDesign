using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Core;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Saving
{
    public class SaveDataProvider : MonoBehaviour
    {
        [Title("Target data provider")]
        [SerializeField] private DataProvider _dataProvider;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getSaveDataManager;
        
        private ISaveDataManager _saveDataManager;
        
        #region Lifecycle
        
        private void Start()
        {
            _getSaveDataManager.RequestDependency(ReceiveSaveDataManager);
        }
        
        private void OnEnable()
        {
            ProvideData();
        }
        
        #endregion
        
        #region Data provider
        
        public void ProvideData()
        {
            if(_dataProvider != null && _saveDataManager != null)
            {
                _dataProvider.ProvideData(new object[] { _saveDataManager.CurrentSaveData });
            }
        }
        
        #endregion
        
        #region Dependencies
        
        private void ReceiveSaveDataManager(object rawObj)
        {
            _saveDataManager = rawObj as ISaveDataManager;
            ProvideData();
        }
        
        #endregion
    
    }
}