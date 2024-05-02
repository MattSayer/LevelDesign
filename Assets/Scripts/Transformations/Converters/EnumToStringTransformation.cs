using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName ="Transformations/Converters/EnumToString", fileName ="EnumToString_Transformation")]
    public class EnumToStringTransformation : Transformation
    {
        public override object TransformInput(object input)
        {
            if(input is Enum)
            {
                return Enum.GetName(input.GetType(),input);
            }
            
            return input;
        }
    }
}