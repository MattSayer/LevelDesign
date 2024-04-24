using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Conditionals
{
    [CreateAssetMenu(menuName ="Conditionals/FloatCheck", fileName ="FloatConditional")]
    public class FloatConditional : ConditionalCheck
    {
        [Title("Float conditions")]
        [SerializeField] private ConditionalOperator _conditionalOperator;
        [SerializeField] private float _conditionalValue;

        public override bool ApplyCheck(object value)
        {
            if(value.GetType() == typeof(float) || value.GetType() == typeof(int))
            {
                float floatVal = (float)value;
                return ApplyConditionalOperator(floatVal, _conditionalOperator, _conditionalValue);
            }
            else
            {
                return true;
            }
        }
    }
}