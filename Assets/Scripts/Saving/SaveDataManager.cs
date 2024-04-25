using AmalgamGames.Audio;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace AmalgamGames.Saving
{
    public class SaveDataManager : MonoBehaviour, ISaveDataManager
    {

        [Title("Dependency provider")]
        [SerializeField] private DependencyRequest _getSaveDataManager;

        // Singleton
        private static SaveDataManager _instance;

        // Constants
        private const int NUM_SAVE_SLOTS = 3;
        private const string GLOBAL_SAVE_FILE = "global";

        // State
        private bool _isSubscribedToDependencyRequests = false;
        private int _currentSaveSlot = 0;
        private float _timeOnLoad = 0;
        private SaveData _currentSaveData;
        private GlobalSaveData _globalSaveData;

        public SaveData CurrentSaveData { get { return _currentSaveData; } }



        #region Lifecycle

        private void Awake()
        {
            if(_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SubscribeToDependencyRequests();

            InitialiseSettings();

        }

        private void OnDestroy()
        {
            SaveGlobalData();
        }

        #endregion

        #region Initialisation

        private void InitialiseSettings()
        {
            _timeOnLoad = Time.time;

            LoadGlobalSaveData();
        }

        #endregion

        #region Saving

        public void SaveToFile()
        {
            // Update the total time played
            float timeSinceLastSave = Time.time - _timeOnLoad;
            _currentSaveData.TotalTimePlayed += timeSinceLastSave;
            _timeOnLoad = Time.time;

            // Flatten current save data object into dictionary
            //Dictionary<string, object> flattenedDictionary = Tools.GetPropertyDictionary(new object[] { _currentSaveData });

            // Write dictionary out to save file
            SaveSystem.SaveFile(GetSaveFileFromSlot(_currentSaveSlot),_currentSaveData);
        }

        public void LoadSaveSlot(int slot)
        {
            // Load save data from file
            _currentSaveData = (SaveData)SaveSystem.LoadFile(GetSaveFileFromSlot(_currentSaveSlot));
            
            // Update current save slot
            _currentSaveSlot = slot;
            
            // Reset time since last load
            _timeOnLoad = Time.time;
        }

        public void CreateNewSave(int slot)
        {
            // Create new save data object
            SaveData saveData = new SaveData();

            // Write out to file
            SaveSystem.SaveFile(GetSaveFileFromSlot(slot), saveData);

            // Update current save data
            _currentSaveData = saveData;

            // Update current save slot
            _currentSaveSlot = slot;
        }

        public LevelSaveData GetLevelSaveData(string levelID)
        {
            foreach(LevelSaveData levelData in _currentSaveData.CompletedLevels)
            {
                if(levelData.LevelID == levelID)
                {
                    return levelData;
                }
            }
            return null;
        }

        public SaveSlotSummary[] GetSaveSlotSummary()
        {
            SaveSlotSummary[] summary = new SaveSlotSummary[NUM_SAVE_SLOTS];
            // Loop through each slot
            for(int i = 0; i < NUM_SAVE_SLOTS; i++)
            {
                SaveData saveData = (SaveData)SaveSystem.LoadFile(GetSaveFileFromSlot(i));
                summary[i] = new SaveSlotSummary(i);
                if(saveData != null)
                {
                    summary[i].NumStarsCollected = saveData.TotalStarsUnlocked;
                    summary[i].NumCollectiblesFound = saveData.TotalCollectiblesFound;
                    summary[i].TotalTimePlayed = saveData.TotalTimePlayed;
                }
            }

            return summary;
        }

        public int GetMostRecentSaveSlot()
        {
            return _globalSaveData.MostRecentSaveSlot;
        }

        private void SaveGlobalData()
        {
            _globalSaveData.MostRecentSaveSlot = _currentSaveSlot;
            SaveSystem.SaveFile(GLOBAL_SAVE_FILE,_globalSaveData);
        }

        private void LoadGlobalSaveData()
        {
            _globalSaveData = (GlobalSaveData)SaveSystem.LoadFile(GLOBAL_SAVE_FILE);
        }

        #endregion

        #region Utility

        private string GetSaveFileFromSlot(int slot)
        {
            return "saveslot_" + slot;
        }

        private void ConvertDictionaryToSaveData(Dictionary<string, object> flattenedDictionary)
        {
            SaveData saveData = new SaveData();
            
            foreach(string key in flattenedDictionary.Keys)
            {
                if(key.Contains("_"))
                {
                    
                }
                else
                {
                    typeof(SaveData).GetProperty(key).SetValue(saveData, flattenedDictionary[key]);
                }
            }
        }

        #endregion


        #region Dependency provider

        private void ProvideDependency(Action<object> callback)
        {
            callback?.Invoke((ISaveDataManager)this);
        }

        private void SubscribeToDependencyRequests()
        {
            if (!_isSubscribedToDependencyRequests)
            {
                _getSaveDataManager.OnDependencyRequest += ProvideDependency;
                _isSubscribedToDependencyRequests = true;
            }
        }

        private void UnsubscribeFromDependencyRequests()
        {
            if (_isSubscribedToDependencyRequests)
            {
                _getSaveDataManager.OnDependencyRequest -= ProvideDependency;
                _isSubscribedToDependencyRequests = false;
            }
        }

        #endregion
    }

    public interface ISaveDataManager
    {
        public void SaveToFile();
        public void LoadSaveSlot(int slot);
        public void CreateNewSave(int slot);
        public LevelSaveData GetLevelSaveData(string levelID);
        public SaveSlotSummary[] GetSaveSlotSummary();
        public SaveData CurrentSaveData { get; }
    }
}