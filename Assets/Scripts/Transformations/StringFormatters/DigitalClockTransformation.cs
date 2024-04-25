using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Utils;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName = "Transformations/String/DigitalClock", fileName = "DigitalClock_Transformation")]
    public class DigitalClockTransformation : Transformation
    {
        public override object TransformInput(object input)
        {
            Type inputType = input.GetType();
            
            if(inputType == typeof(float) || inputType == typeof(int))
            {
                float floatVal = Convert.ToSingle(input);
                
                return Tools.FormatNumberAsClock(floatVal, true);
            }
            
            return input;
        }
    }
}