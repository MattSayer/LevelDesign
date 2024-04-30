using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Config;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName = "Transformations/Character/ExtractCharacterStatsComponent", fileName = "ExtractCharacterStatsComponent_Transformation")]
    public class ExtractCharacterStatsComponentTransformation : Transformation
    {
        [SerializeField] private CharacterStatsComponent _component;
        
        public override object TransformInput(object input)
        {
            if(input.GetType() == typeof(CharacterStats))
            {
                CharacterStats character = (CharacterStats)input;
                
                string fieldName = Enum.GetName(_component.GetType(),_component);
                object characterValue = typeof(CharacterStats).GetProperty(fieldName).GetValue(character, null);
                return characterValue;
            }
            
            return input;
        }
        
    }
    
    public enum CharacterStatsComponent
        {
            CharacterName,
            TurnSpeed,
            HorizontalCameraSpeed,
            VerticalCameraSpeed,
            RocketConfig,
            NudgeForce,
            NudgeJuiceDrainPerSecond,
            SlowmoTimeScale,
            SlowmoJuiceDrainPerSecond,
            Power,
            Control,
            Technique
        }
}