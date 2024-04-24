using AmalgamGames.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Conditionals
{
    [CreateAssetMenu(menuName = "Conditionals/LevelState", fileName = "LevelState_Conditional")]
    public class LevelStateConditional : ConditionalCheck
    {
        [SerializeField] private LevelState _targetState;

        #region Conditional logic
        public override bool ApplyCheck(object value)
        {
            if(value.GetType() == typeof(LevelState))
            {
                LevelState currentState = (LevelState)value;
                if(currentState == _targetState)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        #endregion
    }
}