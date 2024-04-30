using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Config;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName = "Transformations/Character/CharacterStatsFromEnum", fileName = "CharacterStatsFromEnum_Transformation")]
    public class CharacterStatsFromEnumTransformation : Transformation
    {
        public override object TransformInput(object input)
        {
            if(input.GetType() == typeof(RocketCharacter))
            {
                return CharacterStats.GetCharacterStats((RocketCharacter)input);
            }
            
            return input;
        }
    }
}