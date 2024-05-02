using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TerrainUtils;

namespace AmalgamGames.Config
{
    [CreateAssetMenu(menuName = "Config/CharacterStats", fileName = "NewCharacterStats")]
    public class CharacterStats : ScriptableObject
    {
        [Title("Details")]
        [SerializeField] private RocketCharacter _character;
        [SerializeField] private string _characterName;
        [SerializeField] private string _characterClass;
        [Space]
        [Title("Aiming")]
        [SerializeField] private float _turnSpeed = 20;
        [Space]
        [Title("Camera")]
        [SerializeField] private float _horizontalCameraSpeed = 250;
        [SerializeField] private float _verticalCameraSpeed = 200;
        [Space]
        [Title("Rocket stats")]
        [SerializeField] private RocketConfig _rocketConfig;
        [Space]
        [Title("Nudge")]
        [SerializeField] private int _nudgeForce = 2000;
        [SerializeField] private float _nudgeJuiceDrainPerSecond = 10;
        [Space]
        [Title("Slowmo")]
        [SerializeField] private float _slowmoTimeScale = 0.1f;
        [SerializeField] private float _slowmoJuiceDrainPerSecond = 5f;

        // Display stats
        [SerializeField] [ReadOnly]
        private float _power;
        [SerializeField] [ReadOnly]
        private float _control;
        [SerializeField] [ReadOnly]
        private float _technique;

        public RocketCharacter Character { get { return _character; } }
        public string CharacterName { get { return _characterName; } }
        public string CharacterClass { get { return _characterClass; } }
        public float TurnSpeed { get { return _turnSpeed; } }
        public float HorizontalCameraSpeed { get { return _horizontalCameraSpeed; } }
        public float VerticalCameraSpeed { get { return _verticalCameraSpeed; } }
        public RocketConfig RocketConfig { get { return _rocketConfig; } }

        public float LaunchForce { get { return _rocketConfig.PlayerChargeForce + _rocketConfig.MinChargeForce; } }
        public float BurnForce { get { return _rocketConfig.EngineBurnForce; } }
        public float BurnTime { get { return _rocketConfig.EngineBurnTime; } }
        public int NudgeForce { get { return _nudgeForce; } }
        public float NudgeJuiceDrainPerSecond { get { return _nudgeJuiceDrainPerSecond; } }
        public float SlowmoTimeScale { get { return _slowmoTimeScale; } }
        public float SlowmoJuiceDrainPerSecond { get { return _slowmoJuiceDrainPerSecond; } }

        public float Power { get { return _power; } }
        public float Control { get { return _control; } }   
        public float Technique {  get { return _technique; } }  

        private static CharacterStats[] _allCharacters;
        private static Dictionary<RocketCharacter, CharacterStats> _characters;
        private static Dictionary<string, float[]> _minMaxValues;

        private static int _maxStatsScore = 9;

        #region Debug
        [Title("Debug")]
        [Button]
        private void ReloadDatabase()
        {
            LoadCharacters();
        }
        
        #endregion

        #region Static accessors

        public static CharacterStats GetCharacterStats(RocketCharacter character)
        {
            if (_characters == null)
            {
                LoadCharacters();
            }

            if (_characters.ContainsKey(character))
            {
                return _characters[character];
            }

            return null;
        }

        public static CharacterStats[] GetAllCharacters()
        {
            if(_allCharacters == null)
            {
                LoadCharacters();
            }
            return _allCharacters;
        }

        #endregion

        #region Initialisation

        private static void LoadCharacters()
        {
            _characters = new Dictionary<RocketCharacter, CharacterStats>();
            
            _allCharacters = Resources.LoadAll<CharacterStats>("Characters");
            
            foreach (CharacterStats stats in _allCharacters)
            {
                _characters[stats.Character] = stats;
            }

            CalculateDisplayStats();
        }

        private static void CalculateDisplayStats()
        {
            // Calculate min and max values for each field within attribute group
            // Subtract min value from character value, then divide by range (max - min)
            // This produces a normalized rank for that field
            // Calculate average of all normalized ranks within attribute group
            // Multiply by 5 (max score) and round to nearest 0.5

            CalculateMinMaxValues();

            foreach(CharacterStats character in _allCharacters)
            {
                // Power
                float launchScore = CalculateScoreForField(character, CharacterStatsFields.LaunchForce);
                float burnForceScore = CalculateScoreForField(character, CharacterStatsFields.BurnForce);
                float burnTimeScore = CalculateScoreForField(character, CharacterStatsFields.BurnTime);

                float powerScore = (launchScore + burnForceScore + burnTimeScore) / 3f;
                powerScore = (powerScore * (_maxStatsScore-1)) + 1;

                character._power = Mathf.RoundToInt(powerScore);

                // Control
                float turnSpeedScore = CalculateScoreForField(character, CharacterStatsFields.TurnSpeed);
                float horizontalTurnScore = CalculateScoreForField(character, CharacterStatsFields.HorizontalCameraSpeed);
                float verticalTurnScore = CalculateScoreForField(character, CharacterStatsFields.VerticalCameraSpeed);

                float controlScore = (turnSpeedScore + horizontalTurnScore + verticalTurnScore) / 3f;
                controlScore = (controlScore * (_maxStatsScore-1)) + 1;

                character._control = Mathf.RoundToInt(controlScore);

                // Technique
                float nudgeForceScore = CalculateScoreForField(character, CharacterStatsFields.NudgeForce);
                float nudgeDrainScore = 1 - CalculateScoreForField(character, CharacterStatsFields.NudgeJuiceDrainPerSecond);
                float slowmoScore = CalculateScoreForField(character, CharacterStatsFields.SlowmoTimeScale);
                float slowmoDrainScore = 1 - CalculateScoreForField(character, CharacterStatsFields.SlowmoJuiceDrainPerSecond);

                float techniqueScore = (nudgeForceScore + nudgeDrainScore + slowmoScore + slowmoDrainScore) / 4f;
                techniqueScore = (techniqueScore * (_maxStatsScore-1)) + 1;

                character._technique = Mathf.RoundToInt(techniqueScore);
                
            }

        }

        private static void CalculateMinMaxValues()
        {
            if(_minMaxValues == null)
            {
                _minMaxValues = new Dictionary<string, float[]>();
            }

            foreach(string fieldName in Enum.GetNames(typeof(CharacterStatsFields)))
            {
                float minValue = 99999, maxValue = 0;

                foreach (CharacterStats stats in _allCharacters)
                {
                    float thisValue = Convert.ToSingle(typeof(CharacterStats).GetProperty(fieldName).GetValue(stats, null));
                    if (thisValue < minValue)
                    {
                        minValue = thisValue;
                    }
                    if (thisValue > maxValue)
                    {
                        maxValue = thisValue;
                    }
                }

                _minMaxValues[fieldName] = new float[] { minValue, maxValue };
            }
            
            
        }

        private static float CalculateScoreForField(CharacterStats character, CharacterStatsFields field)
        {
            string fieldName = Enum.GetName(field.GetType(),field);
            float characterValue = Convert.ToSingle(typeof(CharacterStats).GetProperty(fieldName).GetValue(character, null));

            float minValue = _minMaxValues[fieldName][0];
            float maxValue = _minMaxValues[fieldName][1];

            float range = maxValue - minValue;
            
            if(range == 0)
            {
                return 0;
            }

            float normalizedRank = (characterValue - minValue) / range;

            return normalizedRank;
        }

        #endregion

        #region Lifecycle

        private void OnValidate()
        {
            LoadCharacters();
        }
    

        #endregion

        private enum CharacterStatsFields
        {
            TurnSpeed,
            HorizontalCameraSpeed,
            VerticalCameraSpeed,
            LaunchForce,
            BurnForce,
            BurnTime,
            NudgeForce,
            NudgeJuiceDrainPerSecond,
            SlowmoTimeScale,
            SlowmoJuiceDrainPerSecond,
        }
    }

    public enum RocketCharacter
    {
        Agile,
        Heavy,
        AllRounder
    }
}