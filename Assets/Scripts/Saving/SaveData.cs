using AmalgamGames.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Saving
{ 
    [Serializable]
    public class SaveData
    { 
        public int LastThemePlayed { get; set; }
        public int TotalStarsUnlocked {  get; set; }
        public List<LevelSaveData> CompletedLevels { get; set; }
        public int TotalCollectiblesFound { get; set; }
        public long TotalTimePlayed { get; set; }
        public bool HasNewTheme { get; set; }


        public SaveData()
        {
            // Initialise with default values
            LastThemePlayed = 0;
            TotalStarsUnlocked = 0;
            TotalCollectiblesFound = 0;
            TotalTimePlayed = 0;
            CompletedLevels = new List<LevelSaveData>();
            HasNewTheme = false;
        }
    }

    public class SaveSlotSummary
    {
        public int SaveSlot { get; set; }
        public int NumStarsCollected { get; set; }
        public int NumCollectiblesFound {  get; set; }
        public long TotalTimePlayed {  get; set; }

        public SaveSlotSummary(int slot)
        {
            SaveSlot = slot;
            NumStarsCollected = 0;
            NumCollectiblesFound = 0;
            TotalTimePlayed = 0;
        }
    }

    [Serializable]
    public class LevelSaveData
    {
        public string LevelID { get; set; }
        public bool HasBeenCompleted { get; set; }
        public int NumAttempts { get; set; }
        public LevelCompletionStats BestScoreRun { get; set; }

        public LevelSaveData(string levelID)
        {
            LevelID = levelID;
            HasBeenCompleted = false;
            NumAttempts = 0;
            BestScoreRun = new LevelCompletionStats();
        }
    }

    [Serializable]
    public class LevelCompletionStats
    {
        public int StarCount { get; set; }
        public int Score { get; set; }
        public float TimeTaken { get; set; }
        public int BonusPoints { get; set; }
        public int NumRespawns { get; set; }
        public int NumLaunches { get; set; }
        public float JuiceRemaining { get; set; }
        public RocketCharacter Character { get; set; }

        public LevelCompletionStats()
        {
            StarCount = 0;
            Score = 0;
            TimeTaken = 0;
            BonusPoints = 0;
            NumRespawns = 0;
            NumLaunches = 0;
            JuiceRemaining = 0;
            Character = RocketCharacter.AllRounder;
        }
    }

    [Serializable]
    public class GlobalSaveData
    {
        public int MostRecentSaveSlot { get; set; }
    }

}